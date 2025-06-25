using Unity.Jobs;
using Unity.Collections;

public partial class CollisionRegistrySystem
{
    private void RemoveCollision()
    {
        Grid.removeCollisions.SwapBuffer();
        var removeBuffer = Grid.removeCollisions.Reader;
        if (removeBuffer.Length == 0)
            return;

        var indexs = new NativeArray<int>(removeBuffer.Length, Allocator.TempJob);

        Dependency = new MapIdToIndexJob<RemoveCollision>
        {
            indexs = indexs,
            idToIndexs = Grid.idToIndexs,
            array = removeBuffer.AsDeferredJobArray(),
        }.Schedule(removeBuffer, 16, Dependency);

        Dependency = new RemoveCollisionJob
        {
            collisions = Grid.gridCollisions,
            idToIndexs = Grid.idToIndexs,
            indexs = indexs,
            removeBuffer = removeBuffer.AsDeferredJobArray(),
            transforms = Grid.collisionTransforms
        }.Schedule(Dependency);

        indexs.Dispose(Dependency);
    }
}