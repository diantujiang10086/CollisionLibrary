using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public delegate void BatchCollisionEvent(NativeArray<int2> collisionInfos);

public class Grid
{

    internal const int capacity = 40960;

    internal static int2 sceneSize;
    internal static float2 invCell;

    internal static BatchCollisionEvent collisionEnter;
    internal static BatchCollisionEvent collisionExit;
    internal static BatchCollisionEvent collisionStay;

    internal static NativeParallelHashSet<int2> collisions = new NativeParallelHashSet<int2>(capacity*2, Allocator.Persistent);
    internal static NativeParallelHashSet<int2> lastCollisions = new NativeParallelHashSet<int2>(capacity*2, Allocator.Persistent);
    internal static NativeList<int2> enterCollisions = new NativeList<int2>(capacity, Allocator.Persistent);
    internal static NativeList<int2> stayCollisions = new NativeList<int2>(capacity, Allocator.Persistent);
    internal static NativeList<int2> exitCollisions = new NativeList<int2>(capacity, Allocator.Persistent);

    internal static NativeList<GridCollision> gridCollisions = new NativeList<GridCollision>(capacity, Allocator.Persistent);
    internal static NativeList<Transform2D> collisionTransforms = new NativeList<Transform2D>(capacity, Allocator.Persistent);
    internal static NativeList<AABB> worldAABBs = new NativeList<AABB>(capacity, Allocator.Persistent);
    internal static NativeList<int2x2> worldAllCells = new NativeList<int2x2>(capacity, Allocator.Persistent);
    internal static NativeList<ShapeWorldVertex> worldVertexs = new NativeList<ShapeWorldVertex>(capacity, Allocator.Persistent);

    internal static NativeList<ShapeProxy> shapeProxies = new NativeList<ShapeProxy>(capacity, Allocator.Persistent);
    internal static NativeParallelHashMap<int, int> shapeProxyMap = new NativeParallelHashMap<int, int>(capacity, Allocator.Persistent);

    internal static NativeParallelHashMap<int, int> idToIndexs = new NativeParallelHashMap<int, int>(capacity, Allocator.Persistent);
    internal static DoubleBufferedMultiHashMap<int2, int> staticMap = new DoubleBufferedMultiHashMap<int2, int>(capacity, Allocator.Persistent);
    internal static DoubleBufferedMultiHashMap<int2, int> dynamicMap = new DoubleBufferedMultiHashMap<int2, int>(capacity, Allocator.Persistent);

    internal static DoubleArrayBuffer<AddCollision> addCollisions = new DoubleArrayBuffer<AddCollision>(capacity);
    internal static DoubleArrayBuffer<UpdateCollision> updateCollisions = new DoubleArrayBuffer<UpdateCollision>(capacity);
    internal static DoubleArrayBuffer<RemoveCollision> removeCollisions = new DoubleArrayBuffer<RemoveCollision>(capacity);

    public static void Initialize(int width, int height, float cellWidth, float cellHeight)
    {
        sceneSize = new int2(width, height);
        invCell = new float2(1f / cellWidth, 1f / cellHeight);
    }

    public static void AddGameObject(CollisionShape collisionShape)
    {
        var addCollision = collisionShape.CreateCollision();
        if (!addCollision.isCreated)
            return;
        GridManager.Instance.AddCollisionShape(collisionShape);
        int index = shapeProxies.Length;
        shapeProxies.Add(collisionShape.GetShapeProxy());
        addCollision.shapeIndex = index;
        WriteAddCollision(addCollision);
    }

    public static int GetShapeProxyIndex(int key)
    {
        shapeProxyMap.TryGetValue(key, out var index);
        return index;
    }

    public static void RegisterShapeProxy(int key, in ShapeProxy shapeProxy)
    {
        var index =  shapeProxies.Length;
        shapeProxies.Add(shapeProxy);
        shapeProxyMap[key] = index;
    }

    public static void WriteAddCollision(AddCollision addCollision)
    {
        addCollisions.Writer.Add(addCollision);
    }

    public static void WriteAddCollision(NativeArray<AddCollision> addCollisionsArray)
    {
        var writer = addCollisions.Writer;
        writer.AddRange(addCollisionsArray);
    }

    public static void WriteUpdateCollision(UpdateCollision collision)
    {
        var writer = updateCollisions.Writer;
        writer.Add(collision);
    }

    public static void WriteUpdateCollision(NativeArray<UpdateCollision> collisions)
    {
        var writer = updateCollisions.Writer;
        writer.AddRange(collisions);
    }

    public static void WriteRemoveCollision(RemoveCollision collision)
    {
        var writer = removeCollisions.Writer;
        writer.Add(collision);
    }

    public static void WriteRemoveCollision(NativeArray<RemoveCollision> collisions)
    {
        var writer = removeCollisions.Writer;
        writer.AddRange(collisions);
    }

    public static void RegisterBatchCollisionEnter(BatchCollisionEvent value)
    {
        collisionEnter += value;
    }

    public static void RegisterBatchCollisionExit(BatchCollisionEvent value)
    {
        collisionExit += value;
    }

    public static void RegisterBatchCollisionStay(BatchCollisionEvent value)
    {
        collisionStay += value;
    }

    public static void UnRegisterBatchCollisionEnter(BatchCollisionEvent value)
    {
        collisionEnter -= value;
    }

    public static void UnRegisterBatchCollisionExit(BatchCollisionEvent value)
    {
        collisionExit -= value;
    }

    public static void UnRegisterBatchCollisionStay(BatchCollisionEvent value)
    {
        collisionStay -= value;
    }

    public static void Dispose()
    {
        foreach(var worldVertex in worldVertexs)
        {
            worldVertex.Dispose();
        }
        worldVertexs.Dispose();
        collisions.Dispose();
        lastCollisions.Dispose();
        enterCollisions.Dispose();
        exitCollisions.Dispose();
        stayCollisions.Dispose();
        worldAABBs.Dispose();
        staticMap.Dispose();
        dynamicMap.Dispose();
        idToIndexs.Dispose();
        shapeProxies.Dispose();
        shapeProxyMap.Dispose();
        worldAllCells.Dispose();
        addCollisions.Dispose();
        gridCollisions.Dispose();
        updateCollisions.Dispose();
        removeCollisions.Dispose();
        collisionTransforms.Dispose();
    }
}
