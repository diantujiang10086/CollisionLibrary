using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct StreamToMapJob : IJobParallelForDefer
{
    [ReadOnly] public NativeStream.Reader reader;
    [ReadOnly] public NativeArray<GridCollision> gridCollisions;
    public NativeParallelMultiHashMap<int2, int>.ParallelWriter map;
    public void Execute(int index)
    {
        var count = reader.BeginForEachIndex(index);
        for (int i = 0; i < count; i++)
        {
            var value = reader.Read<int3>();
            map.Add(value.xy, value.z);
        }
        reader.EndForEachIndex();
    }
}