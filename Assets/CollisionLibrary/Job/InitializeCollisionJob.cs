using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static CellHelper;

[BurstCompile]
struct InitializeCollisionJob : IJobParallelFor
{
    [ReadOnly] public int startIndex;
    [ReadOnly] public int2 sceneSize;
    [ReadOnly] public float2 invCellSize;
    [ReadOnly] public NativeArray<AddCollision> buffer;
    [ReadOnly] public NativeArray<ShapeProxy> shapeProxies;
    
    public NativeArray<AABB> worldAABBs;
    public NativeArray<int2x2> worldAllCells;
    public NativeArray<Transform2D> transforms;
    public NativeArray<GridCollision> gridCollisions;
    public NativeArray<ShapeWorldVertex> worldVertexs;
    public NativeParallelHashMap<int, int>.ParallelWriter map;

    public void Execute(int index)
    {
        var addCollision = buffer[index];
        var collisionIndex = startIndex + index;

        var shapeProxy = shapeProxies[addCollision.shapeIndex];

        var localCorners = CalculateLocalCorners(shapeProxy, out var isCalculateAABB, out var isUpdateRotation);

        map.TryAdd(addCollision.id, collisionIndex);
        
        gridCollisions[collisionIndex] = new GridCollision
        {
            id = addCollision.id,
            shapeIndex = addCollision.shapeIndex,
            isStatic = addCollision.isStatic,
            isCalculateAABB = isCalculateAABB,
            isUpdateRotation = isUpdateRotation,
            localCorners = localCorners
        };

        var transform = new Transform2D { position = addCollision.position, angle = addCollision.angle };
        transform.ApplyRotationMath();
        transforms[collisionIndex] = transform;

        AABB worldAABB = default;
        worldAABB.Compute(transform, localCorners);
        worldAABBs[collisionIndex] = worldAABB;

        GridLocalToCell(worldAABB.LowerBound, invCellSize, sceneSize, out var min);
        GridLocalToCell(worldAABB.UpperBound, invCellSize, sceneSize, out var max);
        worldAllCells[collisionIndex] = new int2x2(min, max);

        var shapeWorldVertex = new ShapeWorldVertex(shapeProxy);
        shapeWorldVertex.Compute(transform, shapeProxy);
        worldVertexs[collisionIndex] = shapeWorldVertex;
    }

    public static float2x4 CalculateLocalCorners(in ShapeProxy shapeProxy, out bool isCalculateAABB, out bool isUpdateRotation)
    {
        ref var vertex = ref shapeProxy.vertex.Value.Array;

        isCalculateAABB = false;
        isUpdateRotation = false;
        float2x4 result = default;
        float2 min = float2.zero, max = float2.zero;
        if (vertex.Length == 0)
            return result;

        if (shapeProxy.type == ShapeType.Circle)
        {
            var radius = shapeProxy.radius;
            float2 center = vertex[0];
            min = center - radius;
            max = center + radius;
        }
        else
        {
            min = vertex[0];
            max = vertex[0];

            isUpdateRotation = true;
            for (int i = 1; i < vertex.Length; i++)
            {
                var position = vertex[i];
                min = math.min(min, position);
                max = math.max(max, position);
            }
        }

        isCalculateAABB = math.any(max - min > float2.zero);
        result.c0 = new float2(min.x, min.y); // Bottom Left
        result.c1 = new float2(max.x, min.y); // Bottom Right
        result.c2 = new float2(max.x, max.y); // Top Right
        result.c3 = new float2(min.x, max.y); // Top Left

        return result;
    }
}
