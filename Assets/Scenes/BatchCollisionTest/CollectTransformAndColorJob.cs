using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
struct CollectTransformAndColorJob : IJobParallelFor
{
    [ReadOnly] public int shapeIndex;
    [ReadOnly] public NativeArray<BatchElement> elements;
    [ReadOnly] public NativeArray<BatchTransform> batchTransforms;
    [ReadOnly] public NativeArray<BatchColor> batchColors;
    public NativeList<BatchDraw>.ParallelWriter draws;
    
    public void Execute(int index)
    {
        var element = elements[index];
        if(element.shapeIndex == shapeIndex)
        {
            draws.AddNoResize(new BatchDraw
            {
                 transform = batchTransforms[index],
                 color = batchColors[index]
            });
        }
    }
}

