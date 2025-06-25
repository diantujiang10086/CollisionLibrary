using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

class BatchAgent : BaseBatchAgent
{
    public void Initialize(int count)
    {
        SetInstanceCount(count);
    }
    public override ShaderProperty[] GetShaderProperties()
    {
        return new ShaderProperty[]
        {
            new ShaderProperty { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Offset = 3 },
            new ShaderProperty { NameID = Shader.PropertyToID("_BaseColor"), Offset = 1 },
        };
    }
    public JobHandle Update(in NativeList<BatchDraw> draws, JobHandle dependency)
    {
        dependency = new UpdatevisibleJob
        {
            count = draws.Length,
            visibleInstances = visibleInstances
        }.Schedule(instanceCountRef.Value, 64);

        return new UpdateBufferJob
        {
            buffer = buffer,
            draws = draws.AsDeferredJobArray(),
            instanceCount = instanceCountRef.Value,
        }.Schedule(draws.Length, 64, dependency);
    }

    [BurstCompile]
    struct UpdatevisibleJob : IJobParallelFor
    {
        public int count;
        [NativeDisableParallelForRestriction] public NativeArray<bool> visibleInstances;
        public void Execute(int index)
        {
            visibleInstances[index] = index < count;
        }
    }

    [BurstCompile]
    struct UpdateBufferJob : IJobParallelFor
    {
        [ReadOnly] public int instanceCount;
        [ReadOnly] public NativeArray<BatchDraw> draws;
        [NativeDisableParallelForRestriction] public NativeArray<float4> buffer;
        public void Execute(int index)
        {
            var draw = draws[index];
            var transform = draw.transform;
            var color = draw.color.color;

            var pos = transform.position;
            var trs = GetTRS(new float3(pos.x, pos.y, 0), quaternion.identity, 1);
            buffer[index * 3 + 0] = trs.c0;
            buffer[index * 3 + 1] = trs.c1;
            buffer[index * 3 + 2] = trs.c2;

            buffer[instanceCount * 3 + index] = color;
        }

        public static float4x3 GetTRS(in float3 pos, in quaternion rot, in float scale)
        {
            var rotMat = math.mul(new float3x3(rot), float3x3.Scale(scale));
            return new float4x3(
                new float4(rotMat.c0.x, rotMat.c0.y, rotMat.c0.z, rotMat.c1.x),
                new float4(rotMat.c1.y, rotMat.c1.z, rotMat.c2.x, rotMat.c2.y),
                new float4(rotMat.c2.z, pos.x, pos.y, pos.z));
        }
    }
}