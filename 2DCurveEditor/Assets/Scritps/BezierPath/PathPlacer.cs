using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPlacer : MonoBehaviour
{
    public float spacing = .1f;
    public float resolution = 1;

    void Start()
    {
        Vector2[] points = FindObjectOfType<PathCreator>().path.CalculateEvenlySpacedPoints(spacing, resolution);

        for (int i = 0; i < points.Length; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = points[i];

            go.transform.localScale = Vector3.one * spacing * .5f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
