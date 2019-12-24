using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightChanger : MonoBehaviour
{
    public float spacing = .1f;
    public float resolution = 1;

    public PathCreator pathCreator;

    Vector2[] points;

    //物体及其初始高度字典
    Dictionary<GameObject, float> objectInitialHeightDic;

    void Awake()
    {
        Path path = GetComponent<PathCreator>().path;

        points = path.CalculateEvenlySpacedPoints(spacing, resolution);

        objectInitialHeightDic = new Dictionary<GameObject, float>();
    }

    void Update()
    {
        if(objectInitialHeightDic.Count > 0)
        {
            foreach (var item in objectInitialHeightDic)
            {
                Transform target = item.Key.transform;
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

        //找出离该点X坐标最近的点
        int nearestIndex = 0;

        for (int i = 1; i < points.Length; i++)
        {
            float dist = Mathf.Abs(points[i].x - x);
            if (dist < Mathf.Abs(points[nearestIndex].x - x))
            {
                nearestIndex = i;
            }
        }

        return points[nearestIndex].y;
    }
}
