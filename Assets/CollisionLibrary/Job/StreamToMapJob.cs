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
    public NativeParallelHashSet<int2>.ParallelWriter mapKeys;
    public void Execute(int index)
    {
        var count = reader.BeginForEachIndex(index);
        for (int i = 0; i < count; i++)
        {
            var value = reader.Read<int3>();
            var key = value.xy;
            map.Add(key, value.z);
            mapKeys.Add(key);
        }
        reader.EndForEachIndex();
    }
}