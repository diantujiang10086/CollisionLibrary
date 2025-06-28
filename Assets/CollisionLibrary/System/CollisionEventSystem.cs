
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
        collisionArray.Dispose(Dependency);
        lastCollisionArray.Dispose(Dependency);

        Dependency.Complete();
        Test();

    }

    private static void Test()
    {
        var collisions = Grid.collisions;
        var exitCollisions = Grid.exitCollisions;
        var enterCollisions = Grid.enterCollisions;
        var stayCollisions = Grid.stayCollisions;

        Grid.collisionExit?.Invoke(exitCollisions.AsArray());
        Grid.collisionEnter?.Invoke(enterCollisions.AsArray());
        Grid.collisionStay?.Invoke(stayCollisions.AsArray());

        (Grid.lastCollisions, Grid.collisions) = (Grid.collisions, Grid.lastCollisions);
        Grid.collisions.Clear();
        enterCollisions.Clear();
        stayCollisions.Clear();
        exitCollisions.Clear();
    }
}