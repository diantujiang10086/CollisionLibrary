using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CalculateExitEventJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> lastCollisions;
    [ReadOnly] public NativeParallelHashSet<int2> collisions;
    public NativeList<int2>.ParallelWriter exitCollisions;
    public void Execute(int index)
    {
        var collision = lastCollisions[index];
        if (!collisions.Contains(collision))
        {
            exitCollisions.AddNoResize(collision);
        }
    }
}
