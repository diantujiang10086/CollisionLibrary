using Unity.Jobs;

public partial class CollisionRegistrySystem
{
    private void AddCollision()
    {
        Grid.addCollisions.SwapBuffer();
        var addBuffer = Grid.addCollisions.Reader;
        if (addBuffer.Length == 0)
            return;

        int startIndex = Grid.gridCollisions.Length;
        var bufferCount = addBuffer.Length;

        Grid.worldAABBs.ResizeUninitialized(startIndex + bufferCount);
        Grid.gridCollisions.ResizeUninitialized(startIndex + bufferCount);
        Grid.collisionTransforms.ResizeUninitialized(startIndex + bufferCount);
        Grid.worldAllCells.ResizeUninitialized(startIndex + bufferCount);
        Grid.worldVertexs.ResizeUninitialized(startIndex + bufferCount);
        Dependency = new InitializeCollisionJob
        {
            startIndex = startIndex,
            invCellSize = Grid.invCell,
            sceneSize = Grid.sceneSize,
            buffer = addBuffer.AsDeferredJobArray(),
            map = Grid.idToIndexs.AsParallelWriter(),
            transforms = Grid.collisionTransforms.AsArray(),
            worldAABBs = Grid.worldAABBs.AsDeferredJobArray(),
            worldVertexs = Grid.worldVertexs.AsDeferredJobArray(),
            shapeProxies = Grid.shapeProxies.AsDeferredJobArray(),
            worldAllCells = Grid.worldAllCells.AsDeferredJobArray(),
            gridCollisions = Grid.gridCollisions.AsDeferredJobArray()
        }.Schedule(bufferCount, 64, Dependency);
    }
}