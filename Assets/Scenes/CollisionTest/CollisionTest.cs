using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class CollisionTest : MonoBehaviour
{
    public GameObject[] prefabs; 
    public int width;
    public int height;
    public int cellWidth;
    public int cellHeight;
    public int instanceCount = 4000;

    List<Data> list = new List<Data>();
    private void Awake()
    {
        Grid.Initialize(width, height, cellWidth, cellHeight);
    }
    private void Start()
    {
        for (int i = 0; i < instanceCount; i++)
        {
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            var instance = GameObject.Instantiate(prefab);
            instance.name = i.ToString();
            instance.transform.position = new float3(Random.Range(0, width), Random.Range(0, height), 0);
            list.Add(new Data(instance.transform, width, height));
        }
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        foreach (var data in list)
        {
            data.Update(deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Vector3 origin = Vector3.zero;
        int cols = Mathf.FloorToInt(width / cellWidth);
        int rows = Mathf.FloorToInt(height / cellHeight);

        for (int y = 0; y <= rows; y++)
        {
            float yPos = y * cellHeight;
            Vector3 start = origin + new Vector3(0, yPos, 0);
            Vector3 end = origin + new Vector3(cols * cellWidth, yPos, 0);
            Gizmos.DrawLine(start, end);
        }

        for (int x = 0; x <= cols; x++)
        {
            float xPos = x * cellWidth;
            Vector3 start = origin + new Vector3(xPos, 0, 0);
            Vector3 end = origin + new Vector3(xPos, rows * cellHeight, 0);
            Gizmos.DrawLine(start, end);
        }

    }

    class Data
    {
        public Transform transform;
        public float2 position;
        public float2 target;
        public float speed = 10;
        private int width;
        private int height;
        public Data(Transform transform, int width,int height)
        {
            this.transform = transform;
            this.width = width;
            this.height = height;
            position = new float2(transform.position.x,transform.position.y);
            UpdateTarget();
        }

        public void Update(float deltaTime)
        {
            var dir = math.normalize(target - position);
            position += dir * (speed * deltaTime);
            transform.position = new Vector3(position.x, position.y);
            var distance = math.distance(position, target);
            if (distance < 0.1f)
            {
                UpdateTarget();
            }

        }

        void UpdateTarget()
        {
            target = new float2(Random.Range(0, width), Random.Range(0, height));
        }
    }
}
