
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(CollisionRegistrySystem))]
partial class CollisionEventTriggerSystem : SystemBase
{
    DoubleListBuffer<int2> enterCollisions;
    DoubleListBuffer<int2> exitCollisions;
    DoubleListBuffer<int2> stayCollisions;
    protected override void OnCreate()
    {
        enterCollisions = new DoubleListBuffer<int2>(Grid.capacity);
        exitCollisions = new DoubleListBuffer<int2>(Grid.capacity);
        stayCollisions = new DoubleListBuffer<int2>(Grid.capacity);

        var enter = enterCollisions.Writer;
        var exit = exitCollisions.Writer;
        var stay = stayCollisions.Writer;
        Grid.enterUpdate = new CollisionEventUpdate
        {
            result = enter.AsParallelWriter()
        };
        Grid.stayUpdate = new CollisionEventUpdate
        {
            result = stay.AsParallelWriter()
        };
        Grid.exitUpdate = new CollisionEventUpdate
        {
            result = exit.AsParallelWriter()
        };
    }

    protected override void OnDestroy()
    {
        enterCollisions.Dispose();
        exitCollisions.Dispose();
        stayCollisions.Dispose();
    }

    protected override void OnUpdate()
    {
        var enter = enterCollisions.Reader;
        var exit = exitCollisions.Reader;
        var stay = stayCollisions.Reader;

        Grid.collisionEnter?.Invoke(enter.AsArray());
        Grid.collisionExit?.Invoke(exit.AsArray());
        Grid.collisionStay?.Invoke(stay.AsArray());

        enterCollisions.SwapBuffer();
        exitCollisions.SwapBuffer();
        stayCollisions.SwapBuffer();

        enter = enterCollisions.Writer;
        exit = exitCollisions.Writer;
        stay = stayCollisions.Writer;

        Grid.enterUpdate = new CollisionEventUpdate
        {
            result = enter.AsParallelWriter()
        };
        Grid.stayUpdate = new CollisionEventUpdate
        {
            result = stay.AsParallelWriter()
        };
        Grid.exitUpdate = new CollisionEventUpdate
        {
            result = exit.AsParallelWriter()
        };

        enter.Clear();
        exit.Clear();
        stay.Clear();
    }
}