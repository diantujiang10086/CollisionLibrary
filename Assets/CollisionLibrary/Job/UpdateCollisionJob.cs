using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct UpdateTransformJob : IJobParallelForDefer
{
    [ReadOnly] public NativeArray<int> indexs;
    [ReadOnly] public NativeArray<UpdateCollision> buffer;
    [ReadOnly] public NativeArray<GridCollision> gridCollisions;

    [NativeDisableParallelForRestriction] public NativeList<Transform2D> transforms;

    public void Execute(int index)
    {
        var collisionIndex = indexs[index];
        var updateCollision = buffer[index];
        var collision = gridCollisions[collisionIndex];

        var transform = new Transform2D { position = updateCollision.position };
        transform.RefreshCachedTrig(updateCollision.angle);
        transforms[collisionIndex] = transform;
    }
}
