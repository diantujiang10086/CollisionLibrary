using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine.UIElements;

struct Transform2D
{
    public float2 position;
    public float angle;
    public float cos;
    public float sin;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyRotationMath()
    {
        var radians = math.radians(angle);
        cos = math.cos(radians);
        sin = math.sin(radians);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshCachedTrig(float angle)
    {
        this.angle = angle;
        var radians = math.radians(angle);
        cos = math.cos(radians);
        sin = math.sin(radians);
    }
}
