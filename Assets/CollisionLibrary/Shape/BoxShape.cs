using Unity.Mathematics;
using UnityEngine;

public class BoxShape : CollisionShape
{
    public float2 size = new float2(1f, 1f);
    
    public override ShapeProxy CreateShapeProxy()
    {
        return ShapeProxy.CreateBox(float2.zero, size);
    }

    protected override void OnDraw()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 1));
    }
}
