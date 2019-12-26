using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BezierPath
{
    [CustomEditor(typeof(PathCreator))]
    public class PathCreatorEditor : Editor
    {
        PathCreator creator;
        Path path;

        //鼠标可点击取消的范围
        const float segmentSelectDistanceThreshold = .1f;

        int selectedSegmentIndex = -1;

        void OnEnable()
        {
            creator = (PathCreator)target;

            if (creator.path == null)
            {
                creator.CreatePath();
            }

            path = creator.path;
        }

        void OnSceneGUI()
        {
            Input();
            Draw();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();

            if (GUILayout.Button("创建新路径"))
            {
                Undo.RecordObject(creator, "创建新路径");
                EditorUtility.SetDirty(creator);
                creator.CreatePath();
                path = creator.path;
            }

            bool isClosed = GUILayout.Toggle(path.IsClosed, "是闭环");
            if (isClosed != path.IsClosed)
            {
                Undo.RecordObject(creator, "切换闭环");
                EditorUtility.SetDirty(creator);
                path.IsClosed = isClosed;
            }

            bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "自动调整控制点");
            if (autoSetControlPoints != path.AutoSetControlPoints)
            {
                Undo.RecordObject(creator, "自动调整控制点");
                EditorUtility.SetDirty(creator);
                path.AutoSetControlPoints = autoSetControlPoints;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();

            }
        }

        //绘制点
        void Draw()
        {
            //先绘制线
            for (int i = 0; i < path.NumOfSegments; i++)
            {
                Vector2[] points = path.GetPointsInSegemnt(i);

                for (int j = 0; j < points.Length; j++)
                {
                    points[j] = (Vector2)creator.transform.position + points[j] * creator.transform.lossyScale.x;
                }

                Color segmentColor = (i == selectedSegmentIndex && Event.current.shift) ? Color.red : Color.green;
                //绘制曲线
                Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);

                //绘制锚点和控制点连线
                Handles.color = Color.black;
                Handles.DrawLine(points[1], points[0]);
                Handles.DrawLine(points[2], points[3]);
            }

            //绘制锚点
            Handles.color = Color.red;
            for (int i = 0; i < path.NumObPoints; i++)
            {
                //锚点是红色，控制点黄色
                Handles.color = i % 3 == 0 ? Color.red : Color.yellow;

                Vector2 pointPos = (Vector2)creator.transform.position + path[i] * creator.transform.lossyScale.x;
                Vector2 newPos = (Handles.FreeMoveHandle(pointPos, Quaternion.identity, .1f, Vector2.zero, Handles.CylinderHandleCap) - creator.transform.position) / creator.transform.lossyScale.x;

                //移动点到新位置
                if (path[i] != newPos)
                {
                    Undo.RecordObject(creator, "Move points");
                    EditorUtility.SetDirty(creator);
                    path.MovePoints(i, newPos);
                }
            }
        }

        void Input()
        {
            Event guiEvent = Event.current;
            Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

            //Shift+鼠标左键点击，创建锚点
            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
            {
                //如果选中了一个区段
                if (selectedSegmentIndex != -1)
                {
                    Undo.RecordObject(creator, "分割区段");
                    EditorUtility.SetDirty(creator);
                    path.InsertSegment(mousePos, selectedSegmentIndex);
                }
                else if (!path.IsClosed)
                {
                    Undo.RecordObject(creator, "添加区段");
                    EditorUtility.SetDirty(creator);
                    path.AddSegment(mousePos);
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
            {
                //鼠标和锚地距离
                float minDistanceToAnchor = .05f;
                int closestAnchorIndex = -1;

                for (int i = 0; i < path.NumObPoints; i++)
                {
                    float distance = Vector2.Distance(mousePos, path[i]);
                    if (distance < minDistanceToAnchor)
                    {
                        minDistanceToAnchor = distance;
                        closestAnchorIndex = i;
                    }
                }

                if (closestAnchorIndex != -1)
                {
                    Undo.RecordObject(creator, "删除段落");
                    path.DeleteSegment(closestAnchorIndex);
                }
            }

            if (guiEvent.type == EventType.MouseMove)
            {
                float minDistanceToSegment = segmentSelectDistanceThreshold;
                int newSelectedSegmentIndex = -1;

                for (int i = 0; i < path.NumOfSegments; i++)
                {
                    Vector2[] points = path.GetPointsInSegemnt(i);

                    for (int j = 0; j < points.Length; j++)
                    {
                        points[j] = (Vector2)creator.transform.position + points[j] * creator.transform.lossyScale.x;
                    }

                    float distance = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
                    if (distance < minDistanceToSegment)
                    {
                        minDistanceToSegment = distance;
                        newSelectedSegmentIndex = i;
                    }
                }

                if (newSelectedSegmentIndex != selectedSegmentIndex)
                {
                    selectedSegmentIndex = newSelectedSegmentIndex;
                    HandleUtility.Repaint();
                }
            }

        }
    }
}

