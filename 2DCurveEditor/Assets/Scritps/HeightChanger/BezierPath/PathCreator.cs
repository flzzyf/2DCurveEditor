using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierPath
{
    public class PathCreator : MonoBehaviour
    {
        [HideInInspector]
        public Path path;

        public Vector2[] Path
        {
            get
            {
                Vector2[] pathPoints = new Vector2[path.NumObPoints];

                for (int i = 0; i < path.NumObPoints; i++)
                {
                    pathPoints[i] = path[i];
                    pathPoints[i] *= transform.lossyScale;
                    pathPoints[i] += (Vector2)transform.position;

                }

                return pathPoints;
            }
        }

        public bool moveCorrespondingControlPoint = true;

        public void CreatePath()
        {
            path = new Path(Vector2.zero);
        }
    }
}

