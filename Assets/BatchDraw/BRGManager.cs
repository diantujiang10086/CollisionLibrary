using System;
using System.Collections.Generic;
using UnityEngine;

internal class BRGManager : MonoBehaviour
{
    private static BRGManager instance;

    private Queue<BRGContainer> queue = new Queue<BRGContainer>();

    public static BRGManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("BRGManager").AddComponent<BRGManager>();
            }
            return instance;
        }
    }

    public void AddBrgContainer(BRGContainer bRGContainer)
    {
        queue.Enqueue(bRGContainer);
    }

    private void Update()
    {
        int count = queue.Count;
        while (count-- > 0)
        {
            try
            {
                var container = queue.Dequeue();
                if (container.IsDisposed)
                    continue;

                queue.Enqueue(container);
                container.Update();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }

    private void OnDestroy()
    {
        int count = queue.Count;
        while (count-- > 0)
        {
            queue.Dequeue()?.Dispose();
        }
    }
}
