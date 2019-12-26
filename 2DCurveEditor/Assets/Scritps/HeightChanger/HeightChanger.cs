using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierPath;

[RequireComponent(typeof(PathCreator))]
[RequireComponent(typeof(BoxCollider2D))]
public class HeightChanger : MonoBehaviour
{
    public float spacing = .1f;
    public float resolution = 1;

    Vector2[] points;

    //物体及其初始高度字典
    Dictionary<GameObject, float> objectInitialHeightDic;

    PathCreator creator;

    void Awake()
    {
        creator = GetComponent<PathCreator>();
        Path path = creator.path;

        points = path.CalculateEvenlySpacedPoints(spacing, resolution);

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = (Vector2)creator.transform.position + points[i] * creator.transform.lossyScale.x;
        }

        objectInitialHeightDic = new Dictionary<GameObject, float>();
    }

    private void OnDrawGizmos()
    {
        creator = GetComponent<PathCreator>();
        Path path = creator.path;
        points = path.CalculateEvenlySpacedPoints(spacing, resolution);

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = (Vector2)creator.transform.position + points[i] * creator.transform.lossyScale.x;
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }

    void Update()
    {
        if(objectInitialHeightDic.Count > 0)
        {
            foreach (var item in objectInitialHeightDic)
            {
                Transform target = item.Key.transform;

                //如果不在点集范围内，跳过
                if (target.position.x < points[0].x || target.position.x > points[points.Length - 1].x)
                    continue;

                target.position = new Vector2(target.position.x, GetHeight(target.position.x));
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        //记录初始高度
        objectInitialHeightDic.Add(col.gameObject, col.transform.position.y);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        //还原初始高度
        Vector2 pos = col.transform.position;
        pos.y = objectInitialHeightDic[col.gameObject];
        col.transform.position = pos;

        objectInitialHeightDic.Remove(col.gameObject);
    }

    //获取相应X坐标的高度
    float GetHeight(float x)
    {
        //找到相邻的两个高度点，根据X坐标比例，设置高度
        //左边的点
        int rightPointIndex = 0;
        for (int i = 0; i < points.Length; i++)
        {
            if(x < points[i].x)
            {
                rightPointIndex = i;

                break;
            }
        }

        int leftPointIndex = rightPointIndex - 1;

        float percent = (x - points[leftPointIndex].x) / (points[rightPointIndex].x - points[leftPointIndex].x);
        float y = (points[rightPointIndex].y - points[leftPointIndex].y) * percent + points[leftPointIndex].y;

        return y;
    }
}
