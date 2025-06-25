using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct RandomMovementJob : IJobParallelFor
{
    public int width;
    public int height;
    public float deltaTime;
    public NativeArray<BatchElement> elements;
    public NativeArray<BatchTransform> transforms;
    public NativeArray<Unity.Mathematics.Random> randoms;
    public NativeArray<UpdateCollision> updateCollisions;
    public void Execute(int index)
    {
        var element = elements[index];
        var transform = transforms[index];
        float2 direction = math.normalize(transform.target - transform.position);
        transform.position += direction * (transform.speed * deltaTime);

        if(math.distance(transform.position, transform.target) < 0.1f)
        {
            var random = randoms[index];
            transform.target = new float2(random.NextFloat(0, width), random.NextFloat(0, height));
            randoms[index] = random;
        }
        updateCollisions[index] = new UpdateCollision { id = element.id, position = transform.position };
        transforms[index] = transform;
    }
}
