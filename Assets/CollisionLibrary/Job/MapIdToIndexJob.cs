using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct MapIdToIndexJob<T> : IJobParallelForDefer where T : unmanaged, IIdentifiable
{
    [ReadOnly] public NativeArray<T> array;
    [ReadOnly] public NativeParallelHashMap<int, int> idToIndexs;
    public NativeArray<int> indexs;

    public void Execute(int index)
    {
        var item = array[index];
        if (idToIndexs.TryGetValue(item.Id, out var collisionIndex))
        {
            indexs[index] = collisionIndex;
        }
    }
}

