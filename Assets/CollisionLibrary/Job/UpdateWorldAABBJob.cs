using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static CellHelper;

[BurstCompile]
struct UpdateWorldAABBJob : IJobParallelFor
{
    [ReadOnly] public int2 sceneSize;
    [ReadOnly] public float2 invCellSize;
    [ReadOnly] public NativeArray<int> indexs;
    [ReadOnly] public NativeArray<GridCollision> gridCollisions;
    [ReadOnly] public NativeArray<Transform2D> transforms;
    [NativeDisableParallelForRestriction] public NativeArray<AABB> worldAABBs;
    [NativeDisableParallelForRestriction] public NativeList<int2x2> worldAllCells;
    public void Execute(int index)
    {
        var collisionIndex = indexs[index];
        var collision = gridCollisions[collisionIndex];

        if (!collision.isCalculateAABB)
            return;

        var transform = transforms[collisionIndex];

        AABB worldAABB = default;
        worldAABB.Compute(transform, collision.localCorners);
        worldAABBs[collisionIndex] = worldAABB;

        GridLocalToCell(worldAABB.LowerBound, invCellSize, sceneSize, out var min);
        GridLocalToCell(worldAABB.UpperBound, invCellSize, sceneSize, out var max);
        worldAllCells[collisionIndex] = new int2x2(min, max);
    }
}
