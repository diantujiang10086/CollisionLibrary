using Codice.Client.BaseCommands;
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

        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefab = prefabs[i];
            var collisionShape = prefab.GetComponent<CollisionShape>();
            var shapeProxy = collisionShape.CreateShapeProxy();
            shapeProxies.Add(shapeProxy);
            Grid.RegisterShapeProxy(i, shapeProxy);
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
                shapeIndex = 0
            };

            var batchTransform = new BatchTransform 
            {
                speed = 10,
                position = new float2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height)),
                target = new float2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height)),
            };

            var color = new BatchColor
            {
                color = new float4(1, 1, 1, 1),
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

        NativeArray<int2> array = new NativeArray<int2>(50, Allocator.TempJob);
        totalId = 10000;
        for(int i = 0; i < 50; i++)
        {
            totalId += 1;// UnityEngine.Random.Range(0, 5);
            array[i] = new int2(totalId,1 );
        }
        OnBatchCollisionEnter(array);
        array.Dispose();
    }

    private void OnDestroy()
    {
        foreach(var shape in shapeProxies)
        {
            shape.Dispose();
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

    JobHandle Dependency;
    private void OnBatchCollisionExit(NativeArray<int2> collisionInfos)
    {
        var collisionIndexCounts = new NativeList<int2>(collisionInfos.Length * 2, Allocator.TempJob);
        var indexMap = new NativeParallelMultiHashMap<int, int>(collisionInfos.Length * 2, Allocator.TempJob);
        var hash = new NativeParallelHashSet<int>(collisionInfos.Length * 2, Allocator.TempJob);
        Dependency = new CollectJob
        {
            idToIndexs = idToIndexs,
            collisionInfos = collisionInfos,
            hash = hash.AsParallelWriter(),
            indexMap = indexMap.AsParallelWriter(),
        }.Schedule(collisionInfos.Length, 64);

        Dependency = new CountIndexJob
        {
            addValue = -1,
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

    void OnBatchCollisionEnter(NativeArray<int2> collisionInfos)
    {
        var collisionIndexCounts = new NativeList<int2>(collisionInfos.Length * 2, Allocator.TempJob);
        var indexMap = new NativeParallelMultiHashMap<int,int>(collisionInfos.Length*2, Allocator.TempJob);
        var hash = new NativeParallelHashSet<int>(collisionInfos.Length*2, Allocator.TempJob);
         Dependency = new CollectJob
        {
            idToIndexs = idToIndexs,
            collisionInfos = collisionInfos,
            hash = hash.AsParallelWriter(),
            indexMap = indexMap.AsParallelWriter(),
        }.Schedule(collisionInfos.Length, 64);
        
        Dependency = new CountIndexJob
        {
            addValue = 1,
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

        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];
            var transform = transforms[i];
            var color = colors[i].color;
            var shape = shapeProxies[element.shapeIndex];
            Gizmos.color = new Color(color.x, color.y, color.z, color.w);
            switch (shape.type)
            {
                case ShapeType.Circle:
                    {
                        var pos = transform.position;
                        Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y), shape.radius);
                    }
                    break;
            }
        }

    }
}

struct BatchElement
{
    public int id;
    public int shapeIndex;
}

struct BatchTransform
{
    public float speed;
    public float2 position;
    public float2 target;
}

struct BatchColor
{
    public int collisionCount;
    public float4 color;
}

[BurstCompile]
struct RandomMovementJob : IJobParallelFor
{
    public int width;
    public int height;
    public float deltaTime;
    public NativeArray<BatchElement> elements;
    public NativeArray<BatchTransform> transforms;
    public NativeArray<Unity.Mathematics.Random> randoms;
    public NativeArray<UpdateCollision> updateCollisions;
    public void Execute(int index)
    {
        var element = elements[index];
        var transform = transforms[index];
        float2 direction = math.normalize(transform.target - transform.position);
        transform.position += direction * (transform.speed * deltaTime);

        if(math.distance(transform.position, transform.target) < 0.1f)
        {
            var random = randoms[index];
            transform.target = new float2(random.NextFloat(0, width), random.NextFloat(0, height));
            randoms[index] = random;
        }
        updateCollisions[index] = new UpdateCollision { id = element.id, position = transform.position };
        transforms[index] = transform;
    }
}

[BurstCompile]
struct CollectJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> collisionInfos;
    [ReadOnly] public NativeParallelHashMap<int, int> idToIndexs;
    public NativeParallelMultiHashMap<int, int>.ParallelWriter indexMap;
    public NativeParallelHashSet<int>.ParallelWriter hash;

    public void Execute(int index)
    {
        var collisionInfo = this.collisionInfos[index];
        if(idToIndexs.TryGetValue(collisionInfo.x, out var elementIndex))
        {
            indexMap.Add(elementIndex, 1);
            hash.Add(elementIndex);
        }

        if (idToIndexs.TryGetValue(collisionInfo.y, out elementIndex))
        {
            indexMap.Add(elementIndex, 1);
            hash.Add(elementIndex);
        }
    }
}

[BurstCompile]
struct CountIndexJob : IJob
{
    [ReadOnly] public int addValue;
    [ReadOnly] public NativeParallelHashSet<int> hash;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> indexMap;
    public NativeList<int2> collisionIndexCounts;
    public void Execute()
    {
        foreach(var key in hash)
        {
            int2 v = new int2(key, 0);
            if(indexMap.TryGetFirstValue(key, out var value, out var iterator))
            {
                do { v.y += addValue; } while (indexMap.TryGetNextValue(out value, ref iterator));
            }
            collisionIndexCounts.AddNoResize(v);
        }
    }
}

[BurstCompile]
struct UpdateColorJob : IJobParallelForDefer
{
    [ReadOnly] public float4 defaultColor;
    [ReadOnly] public float4 collisionColor;
    [ReadOnly] public NativeArray<int2> collisionIndexCounts;
    [NativeDisableParallelForRestriction] public NativeArray<BatchColor> colors;
    public void Execute(int index)
    {
        var collisionIndexCount = collisionIndexCounts[index];
        int elementIndex = collisionIndexCount.x;

        var color = colors[elementIndex];
        color.collisionCount += collisionIndexCount.y;
        color.color = color.collisionCount > 0 ? collisionColor : defaultColor;

        colors[elementIndex] = color;
    }
}

 