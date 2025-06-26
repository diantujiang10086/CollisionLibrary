using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public partial class CollisionRegistrySystem
{
    private void UpdateCollision(in NativeStream stream)
    {
        Grid.updateCollisions.SwapBuffer();

        var updateBuffer = Grid.updateCollisions.Reader;
        if (updateBuffer.Length == 0)
            return;
        var indexs = new NativeArray<int>(updateBuffer.Length, Allocator.TempJob);
        Dependency = new MapIdToIndexJob<UpdateCollision>
        {
            indexs = indexs,
            idToIndexs = Grid.idToIndexs,
            array = updateBuffer.AsDeferredJobArray(),
        }.Schedule(updateBuffer, 16, Dependency);

        Dependency = new UpdateTransformJob
        {
            indexs = indexs,
            transforms = Grid.collisionTransforms,
            buffer = updateBuffer.AsDeferredJobArray(),
            gridCollisions = Grid.gridCollisions.AsDeferredJobArray(),
        }.Schedule(updateBuffer, 8, Dependency);

        Dependency = new UpdateWorldAABBJob
        {
            indexs = indexs,
            invCellSize = Grid.invCell,
            sceneSize = Grid.sceneSize,
            worldAllCells = Grid.worldAllCells,
            worldAABBs = Grid.worldAABBs.AsDeferredJobArray(),
            gridCollisions = Grid.gridCollisions.AsDeferredJobArray(),
            transforms = Grid.collisionTransforms.AsDeferredJobArray(),
        }.Schedule(indexs.Length, 16, Dependency);
        indexs.Dispose(Dependency);

        Dependency = new GridRangeWriteJob
        {
            invCellSize = Grid.invCell,
            sceneSize = Grid.sceneSize,
            writer = stream.AsWriter(),
            gridCollisions = Grid.gridCollisions.AsDeferredJobArray(),
            worldAllCells = Grid.worldAllCells.AsDeferredJobArray(),
        }.Schedule(Grid.gridCollisions, 16, Dependency);

        Dependency = new UpdateWorldVertexJob
        {
            worldVertexs = Grid.worldVertexs,
            shapeProxies = Grid.shapeProxies.AsDeferredJobArray(),
            gridCollisions = Grid.gridCollisions.AsDeferredJobArray(),
            transforms = Grid.collisionTransforms.AsDeferredJobArray(),
        }.Schedule(Grid.gridCollisions, 8, Dependency);
    }
}