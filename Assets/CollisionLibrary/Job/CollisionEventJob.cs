using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

interface ICollisionEvent
{
    void CollisionEvent(int2 value);
}
public struct CollisionEventUpdate : ICollisionEvent
{
    public NativeList<int2>.ParallelWriter result;
    public void CollisionEvent(int2 value)
    {
        result.AddNoResize(value);
    }
}

[BurstCompile]
struct CollisionEventJob<T> : IJobParallelForDefer where T: struct, ICollisionEvent
{
    [ReadOnly] public NativeList<int2> list;
    public T jobData;
    public void Execute(int index)
    {
        jobData.CollisionEvent(list[index]);
    }
}
