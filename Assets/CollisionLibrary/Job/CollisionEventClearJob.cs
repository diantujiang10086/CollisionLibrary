using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CollisionEventClearJob : IJob
{
    public NativeList<int2> enter;
    public NativeList<int2> exit;
    public NativeList<int2> stay;
    public void Execute()
    {
        enter.Clear();
        exit.Clear();
        stay.Clear();
    }
}