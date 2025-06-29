
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct HashClearJob<T> : IJob where T: unmanaged, IEquatable<T>
{
    public NativeParallelHashSet<T> set;
    public void Execute()
    {
        set.Clear();
    }
}
