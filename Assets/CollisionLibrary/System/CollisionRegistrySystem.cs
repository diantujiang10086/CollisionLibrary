using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public partial class CollisionRegistrySystem : SystemBase
{
    protected override void OnCreate()
    {
    }
    protected override void OnDestroy()
    {
        Grid.Dispose();
    }
    protected override void OnUpdate()
    {
        AddCollision();
        RemoveCollision();

        var dynamicMap = Grid.dynamicMap.SwapAndGetWriteBuffer();
        var stream = new NativeStream(Grid.capacity, Allocator.TempJob);

        UpdateCollision(stream);

        NativeList<int2> allCells = new NativeList<int2>(Grid.capacity, Allocator.TempJob);
        NativeParallelHashSet<int2> mapKeys = new NativeParallelHashSet<int2>(Grid.capacity, Allocator.TempJob);
        Dependency = new StreamToMapJob
        {
            reader = stream.AsReader(),
            map = dynamicMap.AsParallelWriter(),
            mapKeys = mapKeys.AsParallelWriter(),
            gridCollisions = Grid.gridCollisions.AsDeferredJobArray(),
        }.Schedule(Grid.gridCollisions, 16, Dependency);

        Dependency = new HashToListJob<int2>
        {
            hash = mapKeys,
            list = allCells,
        }.Schedule(Dependency);

        stream.Dispose(Dependency);
        mapKeys.Dispose(Dependency);


        CollisionEvent(dynamicMap, allCells);
    }
}


