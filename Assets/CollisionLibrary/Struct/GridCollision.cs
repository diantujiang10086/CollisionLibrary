using Unity.Mathematics;

struct GridCollision
{
    public int id;
    public bool isStatic;
    public int shapeIndex;
    public int layer;
    public int collisionMask;
    public bool isUpdateRotation;
    public bool isCalculateAABB;
    public float2x4 localCorners;
}