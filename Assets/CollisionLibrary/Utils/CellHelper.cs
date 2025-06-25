using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
internal static class CellHelper
{
    [BurstCompile]
    public static void GridLocalToCell(in float2 pos, in float2 invCellSize, in int2 sceneSize, out int2 result)
    {
        int2 cell = (int2)math.floor(pos * invCellSize);
        result = math.clamp(cell, int2.zero, sceneSize - 1);
    }
}