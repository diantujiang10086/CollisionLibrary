using System;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

struct ShapeWorldVertex : IDisposable
{
    public UnsafeList<float2> points;

    public ShapeWorldVertex(in ShapeProxy shapeProxy)
    {
        ref var vertex = ref shapeProxy.vertex.Value;
        ref var shapeVertex = ref vertex.Array;
        points = new UnsafeList<float2>(shapeVertex.Length, Allocator.Persistent);
        points.Resize(shapeVertex.Length, NativeArrayOptions.UninitializedMemory);

    }

    public void Compute(in Transform2D transform, in ShapeProxy shapeProxy)
    {
        ref var vertex = ref shapeProxy.vertex.Value;
        ref var shapeVertex = ref vertex.Array;

        Assert.IsTrue(points.Length == shapeVertex.Length, "point length must match shape vertex length");

        var position = transform.position;
        float2x2 rot = new float2x2(transform.cos, -transform.sin, transform.sin, transform.cos);

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = math.mul(rot, shapeVertex[i]) + position;
        }
    }

    public void Dispose()
    {
        points.Dispose();
    }
}