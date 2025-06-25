using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public abstract class BaseBatchAgent : IBatchAgent
{
    private static ShaderProperty[] defaultShaderPropertys = new ShaderProperty[]
    {
        new ShaderProperty { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Offset = 3 }
    };

    protected NativeArray<float4> buffer;
    protected NativeArray<bool> visibleInstances;
    protected NativeReference<int> instanceCountRef;

    public virtual ShaderProperty[] GetShaderProperties()
    {
        return defaultShaderPropertys;
    }

    void IBatchAgent.SetBuffer(NativeArray<float4> buffer, NativeArray<bool> visibleInstances, NativeReference<int> instanceCountRef)
    {
        this.buffer = buffer;
        this.instanceCountRef = instanceCountRef;
        this.visibleInstances = visibleInstances;
    }

    public void SetInstanceCount(int count)
    {
        this.instanceCountRef.Value = count;
    }

    public virtual void Dispose()
    {

    }
}
