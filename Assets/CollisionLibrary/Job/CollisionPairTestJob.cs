using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static AABB;

[BurstCompile]
struct CollisionPairTestJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> allCells;
    [ReadOnly] public NativeArray<GridCollision> collisions;
    [ReadOnly] public NativeArray<AABB> worldAABBs;
    [ReadOnly] public NativeStream.Reader cellCollisions;
    public NativeList<int2>.ParallelWriter collisionIndexs;

    public void Execute(int index)
    {
        int realCount = cellCollisions.BeginForEachIndex(index);
        int count = math.min(realCount, 128);

        var array = new NativeArray<int>(count, Allocator.Temp);
        for (int i = 0; i < count; i++)
            array[i] = cellCollisions.Read<int>();

        for (int i = count; i < realCount; i++)
            _ = cellCollisions.Read<int>();

        cellCollisions.EndForEachIndex();

        for (int i = 0; i < count; i++)
        {
            var aIndex = array[i];
            var a = collisions[aIndex];
            if (a.isStatic)
                continue;

            var aWorldAABB = worldAABBs[aIndex];
            for (int j = i + 1; j < count; j++)
            {
                var bIndex = array[j];
                var b = collisions[bIndex];

                var bWorldAABB = worldAABBs[bIndex];
                if (TestOverlap(aWorldAABB, bWorldAABB))
                {
                    collisionIndexs.AddNoResize(a.id < b.id ? new int2(aIndex, bIndex) : new int2(bIndex, aIndex));
                }
            }
        }
        array.Dispose();
    }

}