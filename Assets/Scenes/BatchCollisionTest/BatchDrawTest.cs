using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public class BatchDrawTest : MonoBehaviour
{
    public int instanceCount = 4000;
    private BatchAgent[] batchAgents;

    NativeArray<BatchElement> elements;
    NativeArray<BatchTransform> transforms;
    NativeArray<BatchColor> colors;

    internal void Set(GameObject[] prefabs, in NativeArray<BatchElement> elements, in NativeArray<BatchTransform> transforms, in NativeArray<BatchColor> colors)
    {
        batchAgents = new BatchAgent[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            batchAgents[i] = BRGSystem.CreateBatchAgent<BatchAgent>(prefabs[i], instanceCount);
            batchAgents[i].Initialize(instanceCount);
        }

        this.elements = elements;
        this.transforms = transforms;
        this.colors = colors;
    }

    private void OnDestroy()
    {
        foreach (var batchAgent in batchAgents)
        {
            batchAgent.Dispose();
        }
    }

    private void Update()
    {
        if (!elements.IsCreated)
            return;
        for (int i = 0; i < batchAgents.Length; i++)
        {
            var batchAgent = batchAgents[i];
            var draw = new NativeList<BatchDraw>(instanceCount, Allocator.TempJob);
            var dependency = new CollectTransformAndColorJob
            {
                shapeIndex = i,
                elements = elements,
                batchColors = colors,
                batchTransforms = transforms,
                draws = draw.AsParallelWriter(),
            }.Schedule(instanceCount, 64);
            dependency.Complete();
            dependency = batchAgent.Update(draw, dependency);
            draw.Dispose(dependency);
            dependency.Complete();
        }
    }
}