using System;
using UnityEngine;

public static class BRGSystem
{
    public static T CreateBatchAgent<T>(GameObject prefab, int maxInstanceCount) where T : IBatchAgent, new()
    {
        var mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        var material = prefab.GetComponent<Renderer>().sharedMaterial;
        return CreateBatchAgent<T>(mesh, material, maxInstanceCount);
    }

    public static T CreateBatchAgent<T>(Mesh mesh, Material material, int maxInstanceCount) where T : IBatchAgent, new()
    {
        var instance = (T)Activator.CreateInstance(typeof(T));
        var brgContainer = new BRGContainer(mesh, material, instance.GetShaderProperties(), maxInstanceCount);
        instance.SetBuffer(brgContainer.GetBuffer(), brgContainer.GetVisibleInstances(), brgContainer.GetInstanceCountRef());
        BRGManager.Instance.AddBrgContainer(brgContainer);
        return instance;
    }
}
