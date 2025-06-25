using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct RemoveCollisionJob : IJob
{
    [ReadOnly] public NativeArray<RemoveCollision> removeBuffer;
    [ReadOnly] public NativeArray<int> indexs;
    public NativeList<GridCollision> collisions;
    public NativeList<Transform2D> transforms;
    public NativeParallelHashMap<int, int> idToIndexs;
    public void Execute()
    {
        for (int i = 0; i < indexs.Length; i++)
        {
            var collisionIndex = indexs[i];
            var lastIndex = collisions.Length - 1;
            if (lastIndex != collisionIndex)
            {
                var collision = collisions[lastIndex];
                collisions[collisionIndex] = collision;
                idToIndexs[collision.id] = collisionIndex;
            }
            collisions.RemoveAt(lastIndex);
            idToIndexs.Remove(removeBuffer[i].id);
        }
    }
}