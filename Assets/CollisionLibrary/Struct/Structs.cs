using Unity.Mathematics;

public struct AddCollision
{
    public bool isStatic;
    public bool isCreated;
    public int id;
    public int shapeIndex;
    public int layer;
    public int collisionMask;
    public float angle;
    public float2 position;
}
public interface IIdentifiable
{
    int Id { get; }
}
public struct UpdateCollision : IIdentifiable
{
    public int id;
    public float2 position;
    public float angle;

    public int Id => id;
}

public struct RemoveCollision : IIdentifiable
{
    public int id;

    public int Id => id;
}

