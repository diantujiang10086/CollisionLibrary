using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

internal class BRGContainer : IDisposable
{
    const int float4Size = 16;
    private bool isDisposed = false;
    private int maxInstanceCount;

    private BatchRendererGroup _brg;
    private int meshInstanceId;
    private int materialInstanceId;
    private BatchID _batchID;
    private BatchMeshID _meshID;
    private BatchMaterialID _materialID;
    private GraphicsBuffer graphicsBuffer;
    private NativeArray<float4> buffer;
    private NativeArray<bool> visibles;
    private NativeReference<int> instanceCountRef;
    private int float4sPerInstance = 0;

    public bool IsDisposed => isDisposed;

    public BRGContainer(Mesh mesh, Material material, ShaderProperty[] shaderProperties, int maxInstanceCount)
    {
        meshInstanceId = mesh.GetInstanceID();
        materialInstanceId = material.GetInstanceID();

        this.maxInstanceCount = maxInstanceCount;
        this.instanceCountRef = new NativeReference<int>(Allocator.Persistent);

        var metadata = new NativeArray<MetadataValue>(shaderProperties.Length, Allocator.Temp);
        uint offset = 0;
        for (int i = 0; i < shaderProperties.Length; i++)
        {
            metadata[i] = new MetadataValue
            {
                NameID = shaderProperties[i].NameID,
                Value = 0x80000000 | offset
            };
            offset += (uint)(maxInstanceCount * shaderProperties[i].Offset * float4Size);
            float4sPerInstance += (int)shaderProperties[i].Offset;
        }

        graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, maxInstanceCount * float4sPerInstance, UnsafeUtility.SizeOf<float4>());
        buffer = new NativeArray<float4>(graphicsBuffer.count, Allocator.Persistent);
        visibles = new NativeArray<bool>(graphicsBuffer.count, Allocator.Persistent);

        _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
        _meshID = _brg.RegisterMesh(mesh);
        _materialID = _brg.RegisterMaterial(material);

        _batchID = _brg.AddBatch(metadata, graphicsBuffer.bufferHandle);
        metadata.Dispose();

        for (int i = 0; i < visibles.Length; i++)
            visibles[i] = true;
    }


    public void Update()
    {
        int instanceCount = math.min(this.instanceCountRef.Value, maxInstanceCount);
        graphicsBuffer.SetData(buffer, 0, 0, instanceCount * float4sPerInstance);
    }

    public NativeArray<float4> GetBuffer()
    {
        return buffer;
    }

    public NativeArray<bool> GetVisibleInstances()
    {
        return visibles;
    }

    public NativeReference<int> GetInstanceCountRef()
    {
        return instanceCountRef;
    }

    public void Dispose()
    {
        _brg.Dispose();
        buffer.Dispose();
        visibles.Dispose();
        graphicsBuffer.Dispose();
        instanceCountRef.Dispose();
    }

    //[BurstCompile]
    private unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup,
        BatchCullingContext cullingContext,
        BatchCullingOutput cullingOutput,
        IntPtr userContext)
    {
        int alignment = UnsafeUtility.AlignOf<long>();

        var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

        drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(), alignment, Allocator.TempJob);
        drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
        drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(instanceCountRef.Value * sizeof(int), alignment, Allocator.TempJob);

        drawCommands->drawCommandPickingInstanceIDs = null;
        drawCommands->drawCommandCount = 1;
        drawCommands->drawRangeCount = 1;
        drawCommands->visibleInstanceCount = (int)instanceCountRef.Value;
        drawCommands->instanceSortingPositions = null;
        drawCommands->instanceSortingPositionFloatCount = 0;

        int index = 0;
        for (int i = 0; i < instanceCountRef.Value; ++i)
        {
            if (visibles[i])
            {
                drawCommands->visibleInstances[index++] = i;
            }
        }

        drawCommands->drawCommands[0] = new BatchDrawCommand
        {
            visibleOffset = 0,
            visibleCount = (uint)index,
            batchID = _batchID,
            materialID = _materialID,
            meshID = _meshID,
            submeshIndex = 0,
            splitVisibilityMask = 0xff,
            flags = 0,
            sortingPosition = 0
        };

        drawCommands->drawRanges[0] = new BatchDrawRange
        {
            drawCommandsBegin = 0,
            drawCommandsCount = 1,
            filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff }
        };


        return default;
    }
}
