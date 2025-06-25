
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public partial class CollisionRegistrySystem
{
    private void CollisionEvent(in NativeParallelMultiHashMap<int2, int> dynamicMap)
    {
        var allCells = CollectAllCells(dynamicMap);

        var cellCollisions = new NativeStream(Grid.capacity, Allocator.TempJob);
        Dependency = new CollisionCellGatherJob
        {
            allCells = allCells,
            cellCollisions = cellCollisions.AsWriter(),
            dynamicMap = dynamicMap
        }.Schedule(allCells.Length, 32, Dependency);

        NativeList<int2> collisionDetectionDatas = new NativeList<int2>(Grid.capacity*4, Allocator.TempJob);
        Dependency = new CollisionPairTestJob
        {
            allCells = allCells,
            cellCollisions = cellCollisions.AsReader(),
            worldAABBs = Grid.worldAABBs.AsDeferredJobArray(),
            collisions = Grid.gridCollisions.AsDeferredJobArray(),
            collisionIndexs = collisionDetectionDatas.AsParallelWriter(),
        }.Schedule(allCells.Length, 16, Dependency);
        allCells.Dispose(Dependency);
        cellCollisions.Dispose(Dependency);

        Dependency = new PreciseCollisionDetectionJob
        {
            shapeProxies = Grid.shapeProxies.AsDeferredJobArray(),
            detectedCollisions = Grid.collisions.AsParallelWriter(),
            worldVertexData = Grid.worldVertexs.AsDeferredJobArray(),
            collisionData = Grid.gridCollisions.AsDeferredJobArray(),
            collisionPairs = collisionDetectionDatas.AsDeferredJobArray(),
        }.Schedule(collisionDetectionDatas, 16, Dependency);
        collisionDetectionDatas.Dispose(Dependency);
        

        ProcessEvent();
    }

    private NativeArray<int2> CollectAllCells(in NativeParallelMultiHashMap<int2, int> dynamicMap)
    {
        NativeList<int2> array = new NativeList<int2>(Allocator.TempJob);
        Dependency = new CollectCellJob
        {
            map = dynamicMap,
            array = array
        }.Schedule(Dependency);

        NativeParallelHashSet<int2> hash = new NativeParallelHashSet<int2>(Grid.capacity, Allocator.TempJob);
        Dependency = new ListDeduplicationJob<int2>
        {
            array = array.AsDeferredJobArray(),
            hash = hash.AsParallelWriter()
        }.Schedule(array, 16, Dependency);
        array.Dispose(Dependency);
        Dependency.Complete();

        NativeArray<int2> allCells = new NativeArray<int2>(hash.Count(), Allocator.TempJob);
        Dependency = new HashToArrayJob<int2>
        {
            hash = hash,
            array = allCells
        }.Schedule(Dependency);
        hash.Dispose(Dependency);
        
        return allCells;
    }

    private  void ProcessEvent()
    {
        Dependency.Complete();
        var collisionArray = new NativeArray<int2>(Grid.collisions.Count(), Allocator.TempJob);
        var lastCollisionArray = new NativeArray<int2>(Grid.lastCollisions.Count(), Allocator.TempJob);

        var collisionArrayJob = new HashToArrayJob<int2>
        {
            hash = Grid.collisions,
            array = collisionArray,
        }.Schedule(Dependency);

        var lastCollisionArrayJob = new HashToArrayJob<int2>
        {
            hash = Grid.lastCollisions,
            array = lastCollisionArray
        }.Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(collisionArrayJob, lastCollisionArrayJob);

        var calculateEnterAndStayEventJob = new CalculateEnterAndStayEventJob
        {
            collisions = collisionArray,
            lastCollisions = Grid.lastCollisions,
            enterCollisions = Grid.enterCollisions.AsParallelWriter(),
            stayCollisions = Grid.stayCollisions.AsParallelWriter()
        }.Schedule(collisionArray.Length, 8, Dependency);

        var calculateExitEventJob = new CalculateExitEventJob
        {
            lastCollisions = lastCollisionArray,
            collisions = Grid.collisions,
            exitCollisions = Grid.exitCollisions.AsParallelWriter()
        }.Schedule(lastCollisionArray.Length, 16, Dependency);

        Dependency = JobHandle.CombineDependencies(calculateEnterAndStayEventJob, calculateExitEventJob);

        if (collisionArray.IsCreated)
            collisionArray.Dispose(Dependency);

        if (lastCollisionArray.IsCreated)
            lastCollisionArray.Dispose(Dependency);

        Dependency.Complete();

        Grid.collisionExit?.Invoke(Grid.exitCollisions.AsArray());
        Grid.collisionEnter?.Invoke(Grid.enterCollisions.AsArray());
        Grid.collisionStay?.Invoke(Grid.stayCollisions.AsArray());

        (Grid.lastCollisions, Grid.collisions) = (Grid.collisions, Grid.lastCollisions);
        Grid.collisions.Clear();
        Grid.enterCollisions.Clear();
        Grid.stayCollisions.Clear();
        Grid.exitCollisions.Clear();
    }
}