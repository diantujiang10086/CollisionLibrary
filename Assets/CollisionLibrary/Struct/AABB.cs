using Unity.Mathematics;

struct AABB
{
    public float2 LowerBound;
    public float2 UpperBound;

    public AABB(float2 min, float2 max)
    : this(ref min, ref max) { }

    public AABB(float2 center, float width, float height)
        : this(center - new float2(width / 2, height / 2), center + new float2(width / 2, height / 2)) { }

    public AABB(ref float2 min, ref float2 max)
    {
        LowerBound = new float2(math.min(min.x, max.x), math.min(min.y, max.y));
        UpperBound = new float2(math.max(min.x, max.x), math.max(min.y, max.y));
    }

    public float Width => UpperBound.x - LowerBound.x;

    public float Height => UpperBound.y - LowerBound.y;

    public float2 Center => 0.5f * (LowerBound + UpperBound);

    public float2 Extents => 0.5f * (UpperBound - LowerBound);

    public void Compute(in Transform2D transform, float2x4 corners)
    {
        float2 min = float2.zero;
        float2 max = float2.zero;

        float2x2 rotation = new float2x2(transform.cos, -transform.sin, transform.sin, transform.cos);

        for (int i = 0; i < 4; ++i)
        {
            float2 world = math.mul(rotation, corners[i]) + transform.position;

            if (i == 0)
            {
                min = max = world;
            }
            else
            {
                min = math.min(min, world);
                max = math.max(max, world);
            }
        }

        LowerBound = min;
        UpperBound = max;
    }

    public static bool TestOverlap(in AABB a, in AABB b)
    {
        float2 d1 = b.LowerBound - a.UpperBound;
        float2 d2 = a.LowerBound - b.UpperBound;

        return d1.x <= 0 && d1.y <= 0 && d2.x <= 0 && d2.y <= 0;
    }
}

