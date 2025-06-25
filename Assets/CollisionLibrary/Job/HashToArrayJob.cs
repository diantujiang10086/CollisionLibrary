using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

[BurstCompile]
struct HashToArrayJob<T> : IJob where T : unmanaged, IEquatable<T>
{
    [ReadOnly] public NativeParallelHashSet<T> hash;
    public NativeArray<T> array;

    public void Execute()
    {
        var tempArray = hash.ToNativeArray(Allocator.Temp);
        NativeArray<T>.Copy(tempArray, array, tempArray.Length);
        tempArray.Dispose();
    }
}