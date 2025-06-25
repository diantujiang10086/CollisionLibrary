using Poly2Tri;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
public struct ShapeVertex
{
    public BlobArray<float2> Array;
}
public enum ShapeType
{
    Circle,
    Box,
    Polygon
}
public struct ShapeProxy : IDisposable
{
    public BlobAssetReference<ShapeVertex> vertex;
    public ShapeType type;
    public float radius;
    /// <summary>
    /// -1 Convex
    /// 0 Circle
    /// >0 Polygon triangle count
    /// </summary>
    public int count;

    public void Dispose()
    {
        if (vertex.IsCreated)
            vertex.Dispose();
    }

    public static ShapeProxy CreateCircle(float2 offset, float radius)
    {
        NativeArray<float2> vertex = new NativeArray<float2>(1, Allocator.Temp);
        vertex[0] = offset;
        return CreateShape(vertex, radius, 0, ShapeType.Circle);
    }

    public static ShapeProxy CreateBox(float2 offset, float2 size)
    {
        NativeArray<float2> vertex = new NativeArray<float2>(4, Allocator.Temp);
        var halfSize = size * 0.5f;
        vertex[0] = offset - halfSize;
        vertex[1] = offset + new float2(halfSize.x, -halfSize.y);
        vertex[2] = offset + halfSize;
        vertex[3] = offset + new float2(-halfSize.x, halfSize.y);

        return CreateShape(vertex, 0, -1, ShapeType.Box);
    }

    public static ShapeProxy CreatePolygon(float2 offset, float2[] points)
    {
        PolygonPoint[] array = new PolygonPoint[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            array[i] = new PolygonPoint(offset.x + points[i].x, offset.y + points[i].y);
        }
        Polygon polygon = new Polygon(array);
        P2T.Triangulate(polygon);

        int index = 0;
        NativeArray<float2> vertex = new NativeArray<float2>(polygon.Triangles.Count * 3, Allocator.Temp);

        foreach (var triangle in polygon.Triangles)
        {
            foreach (var point in triangle.Points)
            {
                vertex[index++] = new float2((float)point.X, (float)point.Y);
            }
        }
        return CreateShape(vertex, 0, polygon.Triangles.Count, ShapeType.Polygon);
    }

    private static ShapeProxy CreateShape(in NativeArray<float2> vertices, float radius, int count, ShapeType type)
    {
        return new ShapeProxy
        {
            type = type,
            vertex = Create(vertices),
            radius = radius,
            count = count
        };
    }

    private static BlobAssetReference<ShapeVertex> Create(in NativeArray<float2> vertices)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<ShapeVertex>();
        var array = builder.Allocate(ref root.Array, vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
            array[i] = vertices[i];

        vertices.Dispose();

        var blob = builder.CreateBlobAssetReference<ShapeVertex>(Allocator.Persistent);
        builder.Dispose();
        return blob;
    }
}
