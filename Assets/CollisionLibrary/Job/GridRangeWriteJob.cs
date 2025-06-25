using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct GridRangeWriteJob : IJobParallelForDefer
{
    [ReadOnly] public int2 sceneSize;
    [ReadOnly] public float2 invCellSize;
    [ReadOnly] public NativeArray<int2x2> worldAllCells;
    [ReadOnly] public NativeArray<GridCollision> gridCollisions;

    public NativeStream.Writer writer;

    public void Execute(int index)
    {
        var collision = gridCollisions[index];
        if (collision.isStatic)
            return;

        var cells = worldAllCells[index];
        int2 min = cells[0];
        int2 max = cells[1];

        writer.BeginForEachIndex(index);

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                writer.Write(new int3(x, y, index));
            }
        }

        writer.EndForEachIndex();
    }
}
