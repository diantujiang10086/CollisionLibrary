using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public partial class CollisionRegistrySystem
{
    private void CollisionEvent(in NativeParallelMultiHashMap<int2, int> dynamicMap, in NativeList<int2> allCells)
    {
        var cellCollisions = new NativeStream(Grid.capacity, Allocator.TempJob);
        Dependency = new CollisionCellGatherJob
        {
            allCells = allCells.AsDeferredJobArray(),
            cellCollisions = cellCollisions.AsWriter(),
            dynamicMap = dynamicMap
        }.Schedule(allCells, 32, Dependency);

        NativeList<int2> collisionDetectionDatas = new NativeList<int2>(Grid.capacity*4, Allocator.TempJob);
        Dependency = new CollisionPairTestJob
        {
            allCells = allCells.AsDeferredJobArray(),
            cellCollisions = cellCollisions.AsReader(),
            worldAABBs = Grid.worldAABBs.AsDeferredJobArray(),
            collisions = Grid.gridCollisions.AsDeferredJobArray(),
            collisionIndexs = collisionDetectionDatas.AsParallelWriter(),
        }.Schedule(allCells, 16, Dependency);
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

    private  void ProcessEvent()
    {
        var collisionArray = new NativeList<int2>(Grid.capacity, Allocator.TempJob);
        var lastCollisionArray = new NativeList<int2>(Grid.capacity, Allocator.TempJob);

        var collisionArrayJob = new HashToListJob<int2>
        {
            hash = Grid.collisions,
            list = collisionArray,
        }.Schedule(Dependency);

        var lastCollisionArrayJob = new HashToListJob<int2>
        {
            hash = Grid.lastCollisions,
            list = lastCollisionArray
        }.Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(collisionArrayJob, lastCollisionArrayJob);

        var calculateEnterAndStayEventJob = new CalculateEnterAndStayEventJob
        {
            collisions = collisionArray.AsDeferredJobArray(),
            lastCollisions = Grid.lastCollisions,
            enterCollisions = Grid.enterCollisions.AsParallelWriter(),
            stayCollisions = Grid.stayCollisions.AsParallelWriter()
        }.Schedule(collisionArray, 8, Dependency);

        var calculateExitEventJob = new CalculateExitEventJob
        {
            lastCollisions = lastCollisionArray.AsDeferredJobArray(),
            collisions = Grid.collisions,
            exitCollisions = Grid.exitCollisions.AsParallelWriter()
        }.Schedule(lastCollisionArray, 16, Dependency);

        Dependency = JobHandle.CombineDependencies(calculateEnterAndStayEventJob, calculateExitEventJob);
        lastCollisionArray.Dispose(Dependency);

        var lastCollisionClearJob = new HashClearJob<int2>
        {
            set = Grid.lastCollisions
        }.Schedule(Dependency);

        var arrayToSetJob = new ArrayToSetJob<int2> 
        { 
             array = collisionArray.AsDeferredJobArray(),
             result = Grid.lastCollisions.AsParallelWriter()
        }.Schedule(collisionArray, 16 , lastCollisionClearJob);

        var collisionClearJob = new HashClearJob<int2>
        {
            set = Grid.collisions
        }.Schedule(Dependency);

        var enterJob = new CollisionEventJob<CollisionEventUpdate>
        {
            list = Grid.enterCollisions,
            jobData = Grid.enterUpdate
        }.Schedule(Grid.enterCollisions, 16, Dependency);

        var exitJob = new CollisionEventJob<CollisionEventUpdate>
        {
            list = Grid.exitCollisions,
            jobData = Grid.exitUpdate
        }.Schedule(Grid.exitCollisions, 16, Dependency);

        var stayJob = new CollisionEventJob<CollisionEventUpdate>
        {
            list = Grid.stayCollisions,
            jobData = Grid.stayUpdate
        }.Schedule(Grid.stayCollisions, 16, Dependency);


        NativeArray<JobHandle> allHandles = new NativeArray<JobHandle>(5, Allocator.TempJob);
        allHandles[0] = arrayToSetJob;
        allHandles[1] = collisionClearJob;
        allHandles[2] = enterJob;
        allHandles[3] = exitJob;
        allHandles[4] = stayJob;

        Dependency = JobHandle.CombineDependencies(allHandles);
        Dependency = new CollisionEventClearJob
        {
            enter = Grid.enterCollisions,
            exit = Grid.exitCollisions,
            stay = Grid.stayCollisions
        }.Schedule(Dependency);

        collisionArray.Dispose(Dependency);
        allHandles.Dispose(Dependency);
    }
}

