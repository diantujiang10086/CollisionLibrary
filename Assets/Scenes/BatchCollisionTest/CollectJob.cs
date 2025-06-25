using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CollectJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> collisionInfos;
    [ReadOnly] public NativeParallelHashMap<int, int> idToIndexs;
    public NativeParallelMultiHashMap<int, int>.ParallelWriter indexMap;
    public NativeParallelHashSet<int>.ParallelWriter hash;

    public void Execute(int index)
    {
        var collisionInfo = this.collisionInfos[index];
        if(idToIndexs.TryGetValue(collisionInfo.x, out var elementIndex))
        {
            indexMap.Add(elementIndex, 1);
            hash.Add(elementIndex);
        }

        if (idToIndexs.TryGetValue(collisionInfo.y, out elementIndex))
        {
            indexMap.Add(elementIndex, 1);
            hash.Add(elementIndex);
        }
    }
}