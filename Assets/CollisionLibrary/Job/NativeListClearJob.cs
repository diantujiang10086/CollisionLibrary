using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct NativeListClearJob<T> : IJob where T: unmanaged
{
    public NativeList<T> list;
    public void Execute()
    {
        list.Clear();
    }
}
