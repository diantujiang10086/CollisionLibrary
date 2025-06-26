using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CollisionCellGatherJob : IJobParallelForDefer
{
    [ReadOnly] public NativeArray<int2> allCells;
    [ReadOnly] public NativeParallelMultiHashMap<int2, int> dynamicMap;
    //[ReadOnly] public NativeParallelMultiHashMap<int2, int> staticMap;
    public NativeStream.Writer cellCollisions;

    public void Execute(int index)
    {
        var cell = allCells[index];
        cellCollisions.BeginForEachIndex(index);

        if (dynamicMap.TryGetFirstValue(cell, out var collisionIndex, out var it))
        {
            do { cellCollisions.Write(collisionIndex); } while (dynamicMap.TryGetNextValue(out collisionIndex, ref it));
        }
        //if (staticMap.TryGetFirstValue(cell, out id, out it))
        //{
        //    do { cellCollisions.Write(id); } while (staticMap.TryGetNextValue(out id, ref it));
        //}
        cellCollisions.EndForEachIndex();
    }
}
