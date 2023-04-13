using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleBlocksTracker : MonoBehaviour
{
    public HashSet<GameObject> visibleGameObjects;

    VisibleBlocksTracker()
    {

    }

    private void Start()
    {
        if (visibleGameObjects == null)
        {

        }
    }

    private void OnBecameVisible()
    {
        if (visibleGameObjects != null)
        {
            visibleGameObjects.Add(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        if (visibleGameObjects != null)
        {
            visibleGameObjects.Remove(gameObject);
        }
    }
}
