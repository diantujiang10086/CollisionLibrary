using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BatchCollisionTest : MonoBehaviour
{
    public Color defaultColor;
    public Color collisionColor;
    public GameObject[] prefabs;
    public int width;
    public int height;
    public int cellWidth;
    public int cellHeight;
    public int instanceCount = 4000;

    BatchAgent[] batchAgents;
    float4 _defaultColor;
    float4 _collisionColor;
    NativeArray<BatchElement> elements;
    NativeArray<BatchTransform> transforms;
    NativeArray<BatchColor> colors;
    NativeArray<Unity.Mathematics.Random> randoms;
    NativeArray<UpdateCollision> updateCollisions;
    NativeParallelHashMap<int, int> idToIndexs;
    List<ShapeProxy> shapeProxies = new List<ShapeProxy>();
    private void Awake()
    {
        _defaultColor = new float4(defaultColor.r, defaultColor.g, defaultColor.b, defaultColor.a);
        _collisionColor = new float4(collisionColor.r, collisionColor.g, collisionColor.b, collisionColor.a);
        Grid.Initialize(width, height, cellWidth, cellHeight);
        Grid.RegisterBatchCollisionEnter(OnBatchCollisionEnter);
        Grid.RegisterBatchCollisionExit(OnBatchCollisionExit);

        transforms = new NativeArray<BatchTransform>(instanceCount, Allocator.Persistent);
        colors = new NativeArray<BatchColor>(instanceCount, Allocator.Persistent);
        randoms = new NativeArray<Unity.Mathematics.Random>(instanceCount, Allocator.Persistent);
        elements = new NativeArray<BatchElement>(instanceCount, Allocator.Persistent);
        updateCollisions = new NativeArray<UpdateCollision>(instanceCount, Allocator.Persistent);
        idToIndexs = new NativeParallelHashMap<int, int>(instanceCount, Allocator.Persistent);

        batchAgents = new BatchAgent[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefab = prefabs[i];
            var collisionShape = prefab.GetComponent<CollisionShape>();
            var shapeProxy = collisionShape.CreateShapeProxy();
            shapeProxies.Add(shapeProxy);
            Grid.RegisterShapeProxy(i, shapeProxy);
            batchAgents[i] = BRGSystem.CreateBatchAgent<BatchAgent>(prefab, instanceCount);
            batchAgents[i].Initialize(instanceCount);
        }
    }

    private void Start()
    {
        int totalId = 10000;

        NativeArray<AddCollision> addCollisions = new NativeArray<AddCollision>(instanceCount, Allocator.Temp);
        for (int i = 0; i < instanceCount; i++)
        {
            var id = totalId++;

            var element = new BatchElement
            {
                id = id,
                shapeIndex = UnityEngine.Random.Range(0, shapeProxies.Count)
            };

            var batchTransform = new BatchTransform 
            {
                speed = 10,
                position = new float2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height)),
                target = new float2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height)),
            };

            var color = new BatchColor
            {
                color = _defaultColor,
            };

            addCollisions[i] = new AddCollision
            {
                id = element.id,
                shapeIndex = 0,
                position = batchTransform.position,
            };

            colors[i] = color;
            elements[i] = element;
            transforms[i] = batchTransform;
            randoms[i] = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1,int.MaxValue));
            idToIndexs.Add(id, i);
        }
        Grid.WriteAddCollision(addCollisions);
        addCollisions.Dispose();
    }

    private void OnDestroy()
    {
        foreach(var shape in shapeProxies)
        {
            shape.Dispose();
        }
        foreach(var batchAgent in batchAgents)
        {
            batchAgent.Dispose();
        }
        transforms.Dispose();
        colors.Dispose();
        randoms.Dispose();
        elements.Dispose();
        idToIndexs.Dispose();
        updateCollisions.Dispose();
    }

    private void LateUpdate()
    {
        new RandomMovementJob
        {
            width = width,
            height = height,
            randoms = randoms,
            elements = elements,
            transforms = transforms,
            deltaTime = Time.deltaTime,
            updateCollisions = updateCollisions,
        }.Schedule(instanceCount, 64).Complete();

        Grid.WriteUpdateCollision(updateCollisions);
    }

    private void Update()
    {
        for (int i = 0; i < batchAgents.Length; i++)
        {
            var batchAgent = batchAgents[i];
            var transformArray = new NativeList<BatchTransform>(instanceCount, Allocator.TempJob);
            var colorArray = new NativeList<BatchColor>(instanceCount, Allocator.TempJob);
            var dependency = new CollectTransformAndColorJob
            {
                shapeIndex = i,
                elements = elements,
                batchColors = colors,
                batchTransforms = transforms,
                colors = colorArray.AsParallelWriter(),
                transforms = transformArray.AsParallelWriter()
            }.Schedule(instanceCount, 64);
            dependency.Complete();

            dependency = batchAgent.Update(transformArray.AsArray(), colorArray.AsArray(), dependency);
            transformArray.Dispose(dependency);
            colorArray.Dispose(dependency);

            dependency.Complete();

        }
    }


    private void OnBatchCollisionExit(in NativeArray<int2> collisionInfos)
    {
        HandleCollision(collisionInfos, -1);
    }

    void OnBatchCollisionEnter(in NativeArray<int2> collisionInfos)
    {
        HandleCollision(collisionInfos, 1);
    }

    private void HandleCollision(in NativeArray<int2> collisionInfos, int value)
    {
        var collisionIndexCounts = new NativeList<int2>(collisionInfos.Length * 2, Allocator.TempJob);
        var indexMap = new NativeParallelMultiHashMap<int, int>(collisionInfos.Length * 2, Allocator.TempJob);
        var hash = new NativeParallelHashSet<int>(collisionInfos.Length * 2, Allocator.TempJob);
        var Dependency = new CollectJob
        {
            idToIndexs = idToIndexs,
            collisionInfos = collisionInfos,
            hash = hash.AsParallelWriter(),
            indexMap = indexMap.AsParallelWriter(),
        }.Schedule(collisionInfos.Length, 64);

        Dependency = new CountIndexJob
        {
            addValue = value,
            hash = hash,
            indexMap = indexMap,
            collisionIndexCounts = collisionIndexCounts,
        }.Schedule(Dependency);
        indexMap.Dispose(Dependency);
        hash.Dispose(Dependency);
        Dependency.Complete();
        Dependency = new UpdateColorJob
        {
            colors = colors,
            defaultColor = _defaultColor,
            collisionColor = _collisionColor,
            collisionIndexCounts = collisionIndexCounts.AsDeferredJobArray(),
        }.Schedule(collisionIndexCounts, 64, Dependency);
        collisionIndexCounts.Dispose(Dependency);
        Dependency.Complete();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Vector3 origin = Vector3.zero;
        int cols = Mathf.FloorToInt(width / cellWidth);
        int rows = Mathf.FloorToInt(height / cellHeight);

        for (int y = 0; y <= rows; y++)
        {
            float yPos = y * cellHeight;
            Vector3 start = origin + new Vector3(0, yPos, 0);
            Vector3 end = origin + new Vector3(cols * cellWidth, yPos, 0);
            Gizmos.DrawLine(start, end);
        }

        for (int x = 0; x <= cols; x++)
        {
            float xPos = x * cellWidth;
            Vector3 start = origin + new Vector3(xPos, 0, 0);
            Vector3 end = origin + new Vector3(xPos, rows * cellHeight, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}

[BurstCompile]
struct CollectTransformAndColorJob : IJobParallelFor
{
    [ReadOnly] public int shapeIndex;
    [ReadOnly] public NativeArray<BatchElement> elements;
    [ReadOnly] public NativeArray<BatchTransform> batchTransforms;
    [ReadOnly] public NativeArray<BatchColor> batchColors;
    public NativeList<BatchTransform>.ParallelWriter transforms;
    public NativeList<BatchColor>.ParallelWriter colors;
    
    public void Execute(int index)
    {
        var element = elements[index];
        var batchTransform = batchTransforms[index];
        var batchColor = batchColors[index];
        if(element.shapeIndex == shapeIndex)
        {
            transforms.AddNoResize(batchTransform);
            colors.AddNoResize(batchColor);
        }
    }
}
