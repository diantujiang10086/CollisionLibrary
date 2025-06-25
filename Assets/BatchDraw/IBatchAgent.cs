using Unity.Collections;
using Unity.Mathematics;

public interface IBatchAgent
{
    ShaderProperty[] GetShaderProperties();
    internal void SetBuffer(NativeArray<float4> buffer, NativeArray<bool> visibleInstances, NativeReference<int> instanceCountRef);
}
