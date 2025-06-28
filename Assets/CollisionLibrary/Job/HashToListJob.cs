using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct HashToListJob<T> : IJob where T : unmanaged, IEquatable<T>
{
    [ReadOnly] public NativeParallelHashSet<T> hash;
    [NativeDisableParallelForRestriction] public NativeList<T> list;
    public void Execute()
    {
        var temp = hash.ToNativeArray(Allocator.Temp);
        list.ResizeUninitialized(temp.Length);
        NativeArray<T>.Copy(temp, list.AsArray(), temp.Length);
        temp.Dispose();
    }
}