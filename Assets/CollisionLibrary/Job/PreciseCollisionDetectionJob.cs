using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
struct PreciseCollisionDetectionJob : IJobParallelForDefer
{
    private const int maxiteration = 20;

    [ReadOnly] public NativeArray<ShapeProxy> shapeProxies;
    [ReadOnly] public NativeArray<GridCollision> collisionData;
    [ReadOnly] public NativeArray<ShapeWorldVertex> worldVertexData;
    [ReadOnly] public NativeArray<int2> collisionPairs;

    public NativeParallelHashSet<int2>.ParallelWriter detectedCollisions;

    public void Execute(int jobIndex)
    {
        var pair = collisionPairs[jobIndex];
        var indexA = pair.x;
        var indexB = pair.y;

        var collisionA = collisionData[indexA];
        var collisionB = collisionData[indexB];

        var proxyA = shapeProxies[collisionA.shapeIndex];
        var proxyB = shapeProxies[collisionB.shapeIndex];

        var vertexA = worldVertexData[indexA];
        var vertexB = worldVertexData[indexB];

        if (TestOverlap(proxyA, proxyB, vertexA, vertexB))
        {
            detectedCollisions.Add(new int2(collisionA.id, collisionB.id));
        }
    }

    public static bool TestOverlap(in ShapeProxy shapeA, in ShapeProxy shapeB, in ShapeWorldVertex vertexA, in ShapeWorldVertex vertexB)
    {
        if(shapeA.type == ShapeType.Circle && shapeB.type == ShapeType.Circle)
        {
            float2 centerA = vertexA.points[0];
            float2 centerB = vertexB.points[0];

            float radiusSum = shapeA.radius + shapeB.radius;
            float2 diff = centerA - centerB;

            return math.lengthsq(diff) <= radiusSum * radiusSum;
        }
        bool isCircleA = shapeA.count == 0;
        bool isConvexA = shapeA.count == -1;

        bool isCircleB = shapeB.count == 0;
        bool isConvexB = shapeB.count == -1;

        int subShapeCountA = isCircleA || isConvexA ? 1 : shapeA.count;
        int subShapeCountB = isCircleB || isConvexB ? 1 : shapeB.count;

        var shapeAVertices = new FixedList64Bytes<float2>();
        var shapeBVertices = new FixedList64Bytes<float2>();

        for (int i = 0; i < subShapeCountA; i++)
        {
            FillVertices(isCircleA, isConvexA, vertexA, i, ref shapeAVertices);

            for (int j = 0; j < subShapeCountB; j++)
            {
                FillVertices(isCircleB, isConvexB, vertexB, j, ref shapeBVertices);

                if (DetectPair(shapeA, shapeAVertices, shapeB, shapeBVertices))
                    return true;
            }
        }

        return false;
    }

    private static void FillVertices(in bool isCircle, in bool isConvex, in ShapeWorldVertex vertexData, int subIndex, ref FixedList64Bytes<float2> shapeVertices)
    {
        shapeVertices.Clear();

        if (isCircle)
        {
            shapeVertices.Add(vertexData.points[0]);
        }
        else if (isConvex)
        {
            var pts = vertexData.points;
            for (int k = 0; k < pts.Length; k++)
                shapeVertices.Add(pts[k]);
        }
        else
        {
            int baseIdx = subIndex * 3;
            shapeVertices.Add(vertexData.points[baseIdx]);
            shapeVertices.Add(vertexData.points[baseIdx + 1]);
            shapeVertices.Add(vertexData.points[baseIdx + 2]);
        }
    }

    private static bool DetectPair(in ShapeProxy proxyA, in FixedList64Bytes<float2> shapeA, in ShapeProxy proxyB, in FixedList64Bytes<float2> shapeB)
    {
        float2 direction = new float2(1, 0);
        float2 supportA = float2.zero;
        float2 searchNormal = direction;
        FixedList64Bytes<float2> simplex = new FixedList64Bytes<float2>();

        supportA = Support(proxyA, shapeA, proxyB, shapeB, direction, searchNormal);
        simplex.Add(supportA);
        direction = -simplex[0];

        bool overlap = false;
        searchNormal = math.normalizesafe(direction);
        for (int iteration = 0; iteration < maxiteration; iteration++)
        {
            supportA = Support(proxyA, shapeA, proxyB, shapeB, direction, searchNormal);
            if (math.dot(supportA, direction) < 0)
                break;

            simplex.Add(supportA);
            if (SimplexContainsOrigin(ref simplex, ref direction))
            {
                overlap = true;
                break;
            }
            searchNormal = math.normalizesafe(direction);
        }

        return overlap;
    }

    private static float2 Support(in ShapeProxy proxyA, in FixedList64Bytes<float2> vertsA, in ShapeProxy proxyB, in FixedList64Bytes<float2> vertsB, in float2 dir, in float2 normDir)
    {
        return SupportPoint(proxyA, vertsA, dir, normDir) - SupportPoint(proxyB, vertsB, -dir, -normDir);
    }

    private static float2 SupportPoint(in ShapeProxy proxy, in FixedList64Bytes<float2> verts, in float2 dir, in float2 normDir)
    {
        if (proxy.type == ShapeType.Circle)
        {
            return verts[0] + normDir * proxy.radius;
        }

        float2 best = verts[0];
        float maxDot = math.dot(best, dir);
        for (int i = 1; i < verts.Length; i++)
        {
            float d = math.dot(verts[i], dir);
            if (d > maxDot)
            {
                maxDot = d;
                best = verts[i];
            }
        }
        return best;
    }

    private static bool SimplexContainsOrigin(ref FixedList64Bytes<float2> simplex, ref float2 direction)
    {
        float2 A = simplex[^1];
        float2 AO = -A;

        if (simplex.Length == 2)
        {
            float2 B = simplex[0];
            float2 AB = B - A;
            direction = TripleProduct(AB, AO, AB);
            return false;
        }

        if (simplex.Length == 3)
        {
            float2 B = simplex[1], C = simplex[0];
            float2 AB = B - A, AC = C - A;
            float2 ABPerp = TripleProduct(AC, AB, AB);
            float2 ACPerp = TripleProduct(AB, AC, AC);

            if (math.dot(ABPerp, AO) > 0)
            {
                simplex.RemoveAt(0);
                direction = ABPerp;
                return false;
            }

            if (math.dot(ACPerp, AO) > 0)
            {
                simplex.RemoveAt(1);
                direction = ACPerp;
                return false;
            }

            return true;
        }

        direction = AO;
        return false;
    }

    private static float2 TripleProduct(in float2 a, in float2 b, in float2 c)
    {
        return b * math.dot(a, c) - a * math.dot(b, c);
    }
}
