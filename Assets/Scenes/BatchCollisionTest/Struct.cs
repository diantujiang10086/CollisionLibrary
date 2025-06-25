using Unity.Mathematics;

struct BatchElement
{
    public int id;
    public int shapeIndex;
}

struct BatchTransform
{
    public float speed;
    public float2 position;
    public float2 target;
}

struct BatchColor
{
    public int collisionCount;
    public float4 color;
}