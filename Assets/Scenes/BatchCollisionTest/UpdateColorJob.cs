using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
