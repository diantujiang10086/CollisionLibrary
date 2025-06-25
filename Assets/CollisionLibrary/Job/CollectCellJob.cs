using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CollectCellJob : IJob
{
    [ReadOnly] public NativeParallelMultiHashMap<int2, int> map;
    public NativeList<int2> array;
    public void Execute()
    {
        var keys = map.GetKeyArray(Allocator.Temp);
        array.ResizeUninitialized(keys.Length);
        NativeArray<int2>.Copy(keys, array.AsArray(), keys.Length);
        keys.Dispose();
    }
}
