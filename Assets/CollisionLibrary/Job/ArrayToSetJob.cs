
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct ArrayToSetJob<T> : IJobParallelForDefer where T : unmanaged, IEquatable<T>
{
    [ReadOnly] public NativeArray<T> array;
    public NativeParallelHashSet<T>.ParallelWriter result;

    public void Execute(int index)
    {
        result.Add(array[index]);
    }
}
