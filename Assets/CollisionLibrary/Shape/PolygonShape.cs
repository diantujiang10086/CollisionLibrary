using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(PolygonCollider2D))]
public class PolygonShape : CollisionShape
{
    private float2[] points;

    public override ShapeProxy CreateShapeProxy()
    {
        var polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider == null || polygonCollider.points.Length < 3)
            return default;

        int length = polygonCollider.points.Length;
        points = new float2[length];
        for (int i = 0; i < length; i++)
        {
            points[i] = polygonCollider.points[i];
        }

        return ShapeProxy.CreatePolygon(float2.zero, points);
    }

    protected override void OnDraw()
    {
        var polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider == null || polygonCollider.points.Length < 3)
            return;

        if (!Application.isPlaying)
        {
            // 编辑器下仅绘制原始 PolygonCollider2D 轮廓
            Gizmos.color = Color.gray;
            var points = polygonCollider.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = transform.TransformPoint(points[i]);
                Vector2 b = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(a, b);
            }
            return;
        }

        if (!shapeProxy.vertex.IsCreated)
            return;

        ref var verts = ref shapeProxy.vertex.Value.Array;

        for (int i = 0; i < verts.Length; i += 3)
        {
            float2 a = verts[i];
            float2 b = verts[i + 1];
            float2 c = verts[i + 2];

            Vector3 v0 = transform.TransformPoint(new Vector3(a.x, a.y, 0));
            Vector3 v1 = transform.TransformPoint(new Vector3(b.x, b.y, 0));
            Vector3 v2 = transform.TransformPoint(new Vector3(c.x, c.y, 0));

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
    }
}
