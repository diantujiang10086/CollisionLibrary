using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct UpdateWorldVertexJob : IJobParallelForDefer
{
    [ReadOnly] public NativeArray<ShapeProxy> shapeProxies;
    [ReadOnly] public NativeArray<GridCollision> gridCollisions;
    [ReadOnly] public NativeArray<Transform2D> transforms;
    [NativeDisableParallelForRestriction] public NativeList<ShapeWorldVertex> worldVertexs;

    public void Execute(int index)
    {
        var collision = gridCollisions[index];
        var transform = transforms[index];
        var shapeProxy = shapeProxies[collision.shapeIndex];
        worldVertexs[index].Compute(transform, shapeProxy);
    }
}

