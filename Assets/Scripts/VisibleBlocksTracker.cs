using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleBlocksTracker : MonoBehaviour
{
    public HashSet<GameObject> visibleGameObjects;

    VisibleBlocksTracker()
    {
        //visibleGameObjects = new HashSet<GameObject>();
    }

    private void Start()
    {
        if (visibleGameObjects == null)
        {
            Debug.Log("VisibleBlocksTracker: visibleGameObjects is null.");
        }
    }

    private void OnBecameVisible()
    {
        if (visibleGameObjects != null)
        {
            visibleGameObjects.Add(gameObject);
            //Debug.Log(visibleGameObjects.Count);
        }
    }

    private void OnBecameInvisible()
    {
        if (visibleGameObjects != null)
        {
            visibleGameObjects.Remove(gameObject);
            //Debug.Log(visibleGameObjects.Count);
        }
    }

    /*public GameObject[] GetVisibleBlocks()
    {
        GameObject[] gameObjects = new GameObject[visibleGameObjects.Count];
        visibleGameObjects.CopyTo(gameObjects, 0);

        return gameObjects;
    }*/
}
