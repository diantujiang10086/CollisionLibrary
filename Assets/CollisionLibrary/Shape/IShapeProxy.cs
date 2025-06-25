using Unity.Mathematics;
using UnityEngine;

public class Element : MonoBehaviour
{
    public int id;
    public float2 position;
    public float2 target;
    public float speed;

    protected Color color = Color.white;
    protected ShapeProxy shapeProxy;
    private int collisionCount;

    public void SetCollisionCount(int value)
    {
        collisionCount += value;
        color = collisionCount > 0 ? Color.cyan : Color.white;
    }

    public void Update()
    {
        
    }

    public ShapeProxy GetShapeProxy()
    {
        return shapeProxy;
    }

    private void OnDestroy()
    {
        shapeProxy.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Draw();
    }

    protected virtual void Draw()
    {

    }
}