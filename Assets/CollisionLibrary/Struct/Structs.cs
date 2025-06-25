using Unity.Mathematics;

public struct AddCollision
{
    public int id;
    public bool isStatic;
    public float2 position;
    public float angle;
    public int shapeIndex;
    public bool isCreated;
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

