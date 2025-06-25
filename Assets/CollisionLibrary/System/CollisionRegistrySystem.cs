using Unity.Entities;

public partial class CollisionRegistrySystem : SystemBase
{
    protected override void OnCreate()
    {
    }
    protected override void OnDestroy()
    {
        Grid.Dispose();
    }
    protected override void OnUpdate()
    {
        AddCollision();
        RemoveCollision();
        var dynamicMap = Grid.dynamicMap.SwapAndGetWriteBuffer();
        UpdateCollision(dynamicMap);
        CollisionEvent(dynamicMap);
    }
}


