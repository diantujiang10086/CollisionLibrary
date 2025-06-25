using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CalculateEnterAndStayEventJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> collisions;
    [ReadOnly] public NativeParallelHashSet<int2> lastCollisions;
    public NativeList<int2>.ParallelWriter enterCollisions;
    public NativeList<int2>.ParallelWriter stayCollisions;
    public void Execute(int index)
    {
        var collision = collisions[index];
        if (!lastCollisions.Contains(collision))
        {
            enterCollisions.AddNoResize(collision);
        }
        else
        {
            stayCollisions.AddNoResize(collision);
        }
    }
}
