using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct ListDeduplicationJob<T> : IJobParallelForDefer where T : unmanaged, IEquatable<T>
{
    [ReadOnly] public NativeArray<T> array;
    public NativeParallelHashSet<T>.ParallelWriter hash;
    public void Execute(int index)
    {
        hash.Add(array[index]);
    }
}
