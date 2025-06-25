using UnityEngine;

[RequireComponent(typeof(CollisionShape))]
public class CollisionColor : MonoBehaviour, ICollisionEnter, ICollisionExit
{
    public Color defaultColor = Color.white;
    public Color collisionColor = Color.white;
    private CollisionShape shape;
    private int count;
    private MeshRenderer meshRenderer;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        shape = GetComponent<CollisionShape>();
        shape.SetDrawColor(defaultColor);
    }

    void ICollisionEnter.CollisionEnter(int a, int b)
    {
        UpdateCount(1);
    }

    void ICollisionExit.CollisionExit(int a, int b)
    {
        UpdateCount(-1);
    }

    private void UpdateCount(int value)
    {
        count += value;
        shape.SetDrawColor(count <= 0 ? defaultColor : collisionColor);
        if(meshRenderer != null)
        {
            meshRenderer.material.color = shape.DrawColor;
        }
    }
}