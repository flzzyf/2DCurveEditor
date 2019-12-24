using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    List<Vector2> points;

    [SerializeField, HideInInspector]
    bool autoSetControlPoints;

    public bool AutoSetControlPoints
    {
        get
        {
            return autoSetControlPoints;
        }
        set
        {
            if(autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if(autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }

    [SerializeField, HideInInspector]
    bool isClosed;

    public bool IsClosed
    {
        get
        {
            return isClosed;
        }
        set
        {
            if(isClosed != value)
            {
                isClosed = value;

                if (isClosed)
                {
                    //变成闭环
                    //添加两个控制点
                    points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                    points.Add(points[0] * 2 - points[1]);

                    if (autoSetControlPoints)
                    {
                        AutoSetAnchorControlPoints(0);
                        AutoSetAnchorControlPoints(points.Count - 3);
                    }
                }
                else
                {
                    //变成开放路径
                    points.RemoveRange(points.Count - 2, 2);

                    if (autoSetControlPoints)
                    {
                        AutoSetStartAndEndControls();
                    }
                }
            }
        }
    }

    //在左右创建两个锚点和控制点，序号为0123
    public Path(Vector2 center)
    {
        points = new List<Vector2>
        {
            center + Vector2.left,
            center + (Vector2.left + Vector2.up) * .5f,
            center + (Vector2.right + Vector2.down) * .5f,
            center +Vector2.right
        };
    }

    //获取点
    public Vector2 this[int index] { get { return points[index]; } }

    //返回点的数量
    public int NumObPoints { get { return points.Count; } }

    //返回段落数量
    public int NumOfSegments
    {
        get
        {
            return points.Count / 3;
        }
    }

    //添加新段落，即创建两个控制点和一个锚点，序号为456
    public void AddSegment(Vector2 anchorPos)
    {
        //4号控制点是2号控制点对面
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        //5号控制点是4号和6号中间
        points.Add((points[points.Count - 1] + anchorPos) * .5f);

        points.Add(anchorPos);

        if(autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }
    }

    //插入新段落
    public void InsertSegment(Vector2 anchorPos, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector2[] { Vector2.zero, anchorPos, Vector2.zero });
        if(AutoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }
    }

    //删除段落
    public void DeleteSegment(int anchorIndex)
    {
        //段落数量大于2或者不是闭环才能删除
        if(NumOfSegments > 2 || (!isClosed && NumOfSegments > 1))
        {
            if (anchorIndex == 0)
            {
                if (isClosed)
                {
                    points[points.Count - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !isClosed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
        }
    }

    //获取一个段落中的所有点
    public Vector2[] GetPointsInSegemnt(int index)
    {
        return new Vector2[] { points[index * 3], points[index * 3 + 1], points[index * 3 + 2], points[LoopIndex(index * 3 + 3)] };
    }

    //移动点到目标位置
    public void MovePoints(int index, Vector2 pos)
    {
        //移动量
        Vector2 deltaMove = pos - points[index];

        //未开启自动调整锚点，或者是锚点，才能移动
        if(IsAnchorPoint(index) || !autoSetControlPoints)
            points[index] = pos;

        if(autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(index);

            return;
        }

        //如果是锚点，同时也移动它的控制点
        if(IsAnchorPoint(index))
        {
            if(IsPointExist(index + 1))
                points[LoopIndex(index + 1)] += deltaMove;
            if(IsPointExist(index - 1))
                points[LoopIndex(index - 1)] += deltaMove;
        }
        else
        {
            //如果是控制点，同时移动它相对的控制点
            bool nextPointsIsAnchor = IsAnchorPoint(index + 1);
            //相对的控制点
            int correspindingControlIndex = (nextPointsIsAnchor) ? index + 2 : index - 2;
            int anchorIndex = nextPointsIsAnchor ? index + 1 : index - 1;

            //相对控制点存在，移动
            if(IsPointExist(correspindingControlIndex))
            {
                anchorIndex = LoopIndex(anchorIndex);
                correspindingControlIndex = LoopIndex(correspindingControlIndex);
                float distance = (points[anchorIndex] - pos).magnitude;
                Vector2 dir = (points[anchorIndex] - pos).normalized;
                points[correspindingControlIndex] = points[anchorIndex] + distance * dir;
            }
        }
    }

    //计算出曲线上的均匀分布的点集
    public Vector2[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1)
    {
        List<Vector2> evenlySpacedPoints = new List<Vector2>();
        evenlySpacedPoints.Add(points[0]);
        Vector2 previousPoint = points[0];
        float distanceSinceLastPoint = 0;

        //遍历每一段
        for (int segmentIndex = 0; segmentIndex < NumOfSegments; segmentIndex++)
        {
            Vector2[] ps = GetPointsInSegemnt(segmentIndex);

            //估计曲线长度
            float controlNetLength = Vector2.Distance(ps[0], ps[1]) + Vector2.Distance(ps[1], ps[2]) +
                Vector2.Distance(ps[1], ps[2]);
            float estimatedCurveLength = Vector2.Distance(ps[0], ps[3]) + controlNetLength / 2f;

            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);

            float t = 0;

            while(t <= 1)
            {
                t += 1f / divisions;
                Vector2 pointOnCurve = Bezier.EvaluateCubic(ps[0], ps[1], ps[2], ps[3], t);
                distanceSinceLastPoint += Vector2.Distance(pointOnCurve, previousPoint);

                while(distanceSinceLastPoint >= spacing)
                {
                    float overShotDistance = distanceSinceLastPoint - spacing;
                    Vector2 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overShotDistance;
                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    distanceSinceLastPoint = overShotDistance;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }
        }

        return evenlySpacedPoints.ToArray();
    }

    #region 自动调整控制点位置

    //自动设置控制点到合适位置
    void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector2 anchorPos = points[anchorIndex];
        Vector2 dir = Vector2.zero;
        //和相邻锚点的距离
        float[] neighbourDistances = new float[2];

        //不是第一个锚点或者是闭环
        if(anchorIndex - 3 >= 0 || isClosed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        if (anchorIndex + 3 >= 0 || isClosed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        //调整两个控制点
        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if(IsPointExist(controlIndex))
            {
                points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * .5f;
            }
        }
    }

    //自动调整起始和终末控制点
    void AutoSetStartAndEndControls()
    {
        if(!isClosed)
        {
            points[1] = (points[0] + points[2]) * .5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f;
        }
    }

    //自动设置所有控制点
    void AutoSetAllControlPoints()
    {
        for (int i = 0; i < points.Count; i += 3)
        {
            AutoSetAnchorControlPoints(i);
        }

        AutoSetStartAndEndControls();
    }

    //自动设置相关控制点
    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        for (int i = updatedAnchorIndex - 3; i < updatedAnchorIndex + 3; i += 3)
        {
            if(IsPointExist(i))
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }

        AutoSetStartAndEndControls();
    }

    #endregion

    #region 帮助方法

    //是锚点
    bool IsAnchorPoint(int index)
    {
        if(index % 3 == 0)
        {
            return true;
        }

        return false;
    }

    //判断点存在
    bool IsPointExist(int index)
    {
        return (index >= 0 && index < points.Count) || isClosed;
    }

    //在范围内循环一个序号
    int LoopIndex(int index)
    {
        return (index + points.Count) % points.Count;
    }

    #endregion
}
