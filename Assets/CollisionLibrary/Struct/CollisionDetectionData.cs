using Unity.Mathematics;

struct CollisionDetectionData
{
    public int aIndex;
    public int bIndex;
    public int2 collisionIds;
    public AABB aAABB;
    public AABB bAABB;
    public ShapeProxy aShapeProxy;
    public ShapeProxy bShapeProxy;
    public ShapeWorldVertex aShapeWorldVertex;
    public ShapeWorldVertex bShapeWorldVertex;
}
