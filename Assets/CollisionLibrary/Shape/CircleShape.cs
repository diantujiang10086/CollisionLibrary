using Unity.Mathematics;
using UnityEngine;

public class CircleShape : CollisionShape
{
    public float radius = 0.5f;

    public override ShapeProxy CreateShapeProxy()
    {
        return ShapeProxy.CreateCircle(float2.zero, radius);
    }

    protected override void OnDraw()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
