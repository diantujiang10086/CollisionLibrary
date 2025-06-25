using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct CountIndexJob : IJob
{
    [ReadOnly] public int addValue;
    [ReadOnly] public NativeParallelHashSet<int> hash;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> indexMap;
    public NativeList<int2> collisionIndexCounts;
    public void Execute()
    {
        foreach(var key in hash)
        {
            int2 v = new int2(key, 0);
            if(indexMap.TryGetFirstValue(key, out var value, out var iterator))
            {
                do { v.y += addValue; } while (indexMap.TryGetNextValue(out value, ref iterator));
            }
            collisionIndexCounts.AddNoResize(v);
        }
    }
}
