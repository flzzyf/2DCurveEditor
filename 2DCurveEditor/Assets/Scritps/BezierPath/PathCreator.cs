using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCreator : MonoBehaviour
{
    [HideInInspector]
    public Path path;

    public bool moveCorrespondingControlPoint = true;

    public void CreatePath()
    {
        path = new Path(Vector2.zero);
    }
}
