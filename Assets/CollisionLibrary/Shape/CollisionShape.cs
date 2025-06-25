using UnityEngine;

public class CollisionShape : MonoBehaviour
{
    public int collisionMask = -1;
    protected int id;
    protected Color drawColor = Color.white;
    protected UpdateCollision updateCollision;
    protected ShapeProxy shapeProxy;

    public int Id => id;

    public Color DrawColor => drawColor;

    private void Awake()
    {
        Grid.AddGameObject(this);
    }
    private void OnDestroy()
    {
        shapeProxy.Dispose();
    }

    private void Update()
    {
        var pos = transform.position;
        updateCollision.position = new Unity.Mathematics.float2(pos.x, pos.y);
        updateCollision.id = id;
        updateCollision.angle = transform.eulerAngles.z;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = drawColor;
        OnDraw();
    }

    public void SetDrawColor(Color color)
    {
        drawColor = color;
    }

    public AddCollision CreateCollision()
    {
        shapeProxy = CreateShapeProxy();
        if (!shapeProxy.vertex.IsCreated)
            return default;
        id = transform.GetInstanceID();
        var pos = transform.position;
        return new AddCollision
        {
            id = id,
            isCreated = true,
            layer = gameObject.layer,
            collisionMask = collisionMask,
            isStatic = gameObject.isStatic,
            angle = transform.eulerAngles.z,
            position = new Unity.Mathematics.float2(pos.x, pos.y),
        };
    }

    public ShapeProxy GetShapeProxy()
    {
        return shapeProxy;
    }

    public UpdateCollision GetUpdateCollision()
    {
        return updateCollision;
    }

    public virtual ShapeProxy CreateShapeProxy()
    {
        return default;
    }

    protected virtual void OnDraw()
    {

    }
}
