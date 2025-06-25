using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

internal class GridManager : MonoBehaviour
{
    private static GridManager instance;
    public static GridManager Instance
    {
        get
        {
            if (instance == null)
            {
                var gameObject = new GameObject("GridManager");
                if (Application.isPlaying)
                    GameObject.DontDestroyOnLoad(gameObject);
                instance = gameObject.AddComponent<GridManager>();
            }
            return instance;
        }
    }

    class Collision
    {
        public CollisionShape collisionShape;
        public ICollisionEnter[] collisionEnter;
        public ICollisionExit[] collisionExit;
        public ICollisionStay[] collisionStay;

        public int Id => collisionShape.Id;

        public void CollisionEnter(int a, int b)
        {
            foreach (var collisionEvent in collisionEnter)
            {
                collisionEvent?.CollisionEnter(a, b);
            }
        }
        public void CollisionExit(int a, int b)
        {
            foreach (var collisionEvent in collisionExit)
            {
                collisionEvent?.CollisionExit(a, b);
            }
        }
        public void CollisionStay(int a, int b)
        {
            foreach (var collisionEvent in collisionStay)
            {
                collisionEvent?.CollisionStay(a, b);
            }
        }
    }


    private Dictionary<int, int> shapeToIndexs = new Dictionary<int, int>();
    private List<Collision> collision = new List<Collision>();
    private NativeList<UpdateCollision> updateCollisions;

    private void Awake()
    {
        updateCollisions = new NativeList<UpdateCollision>(Grid.capacity, Allocator.Persistent);
        Grid.RegisterBatchCollisionEnter(BatchCollisionsEnter);
        Grid.RegisterBatchCollisionExit(BatchCollisionsExit);
        Grid.RegisterBatchCollisionStay(BatchCollisionsStay);
    }

    private void OnDestroy()
    {
        Grid.UnRegisterBatchCollisionEnter(BatchCollisionsEnter);
        Grid.RegisterBatchCollisionExit(BatchCollisionsExit);
        Grid.RegisterBatchCollisionStay(BatchCollisionsStay);

        if (updateCollisions.IsCreated)
            updateCollisions.Dispose();
    }

    public void AddCollisionShape(CollisionShape collisionShape)
    {
        var collision = new Collision
        {
            collisionShape = collisionShape,
            collisionEnter = collisionShape.GetComponents<ICollisionEnter>(),
            collisionExit = collisionShape.GetComponents<ICollisionExit>(),
            collisionStay = collisionShape.GetComponents<ICollisionStay>()
        };
        shapeToIndexs[collisionShape.Id] = this.collision.Count;
        this.collision.Add(collision);
        int index = updateCollisions.Length;
        updateCollisions.ResizeUninitialized(updateCollisions.Length + 1);
        updateCollisions[index] = collisionShape.GetUpdateCollision();

    }

    public void RemoveCollisionShape(CollisionShape collisionShape)
    {
        if (!shapeToIndexs.TryGetValue(collisionShape.Id, out int index))
            return;

        int lastIndex = collision.Count - 1;

        if (index != lastIndex)
        {
            var lastShape = collision[lastIndex];
            collision[index] = lastShape;

            updateCollisions[index] = updateCollisions[lastIndex];

            shapeToIndexs[lastShape.Id] = index;
        }

        collision.RemoveAt(lastIndex);
        updateCollisions.Resize(updateCollisions.Length - 1, NativeArrayOptions.UninitializedMemory);

        shapeToIndexs.Remove(collisionShape.Id);
    }

    private void Update()
    {
        for (int i = 0; i < collision.Count; i++)
        {
            updateCollisions[i] = collision[i].collisionShape.GetUpdateCollision();
        }
        Grid.WriteUpdateCollision(updateCollisions.AsArray());
    }

    public void BatchCollisionsExit(in NativeArray<int2> collisions)
    {
        foreach (var collision in collisions)
        {
            var a = collision.x;
            var b = collision.y;
            if (shapeToIndexs.TryGetValue(a, out var index))
            {
                this.collision[index].CollisionExit(a, b);
            }

            if (shapeToIndexs.TryGetValue(b, out index))
            {
                this.collision[index].CollisionExit(a, b);
            }
        }
    }

    public void BatchCollisionsStay(in NativeArray<int2> collisions)
    {
        foreach (var collision in collisions)
        {
            var a = collision.x;
            var b = collision.y;
            if (shapeToIndexs.TryGetValue(a, out var index))
            {
                this.collision[index].CollisionStay(a, b);
            }

            if (shapeToIndexs.TryGetValue(b, out index))
            {
                this.collision[index].CollisionStay(a, b);
            }
        }
    }

    public void BatchCollisionsEnter(in NativeArray<int2> collisions)
    {
        foreach (var collision in collisions)
        {
            var a = collision.x;
            var b = collision.y;
            if (shapeToIndexs.TryGetValue(a, out var index))
            {
                this.collision[index].CollisionEnter(a, b);
            }

            if (shapeToIndexs.TryGetValue(b, out index))
            {
                this.collision[index].CollisionEnter(a, b);
            }
        }

    }
}