using System.Collections.Generic;
using UnityEngine;

public enum AttackCheckShape
{
    Point,
    Circle,
    Box,
    Capsule,
    Line,
    Arc,
    Polygon
}

[System.Serializable]
public class AttackCheck
{
    [Header("Identity")]
    public string label;
    public List<int> attackPoints = new();
    public AttackCheckShape shape = AttackCheckShape.Point;

    [Tooltip("Anchor for this check (bone/empty).")]
    public Transform checkTransform;

    [Header("Gizmos")]
    [Tooltip("(Unused for gating; kept for compatibility)")]
    public bool debugDraw = true;

    // Circle
    [HideInInspector] public float circleRadius = 0.5f;

    // Box (2D)
    [HideInInspector] public Vector2 boxSize = new Vector2(1f, 1f);

    // Capsule (2D)
    public enum CapsuleDirection { Horizontal, Vertical }
    [HideInInspector] public Vector2 capsuleSize = new Vector2(1.5f, 0.6f);
    [HideInInspector] public CapsuleDirection capsuleDirection = CapsuleDirection.Horizontal;

    // Line (segment + thickness)
    [HideInInspector] public float lineLength = 1.0f;
    [HideInInspector] public float lineThickness = 0.1f;

    // Arc (sector)
    [HideInInspector] public float arcRadius = 1.0f;
    [HideInInspector] public float arcAngleDegrees = 90f;
    [HideInInspector] public float arcInnerRadius = 0f; // 0 = solid sector; >0 = ring sector

    // Polygon (2D local points)
    [HideInInspector]
    public List<Vector2> polygonPoints = new()
    {
        new(-0.5f, -0.5f), new(0.5f, -0.5f), new(0.5f, 0.5f), new(-0.5f, 0.5f)
    };

#if UNITY_EDITOR
    public void DrawGizmos()
    {
        if (checkTransform == null) return; // require an anchor

        var prevColor = UnityEditor.Handles.color;
        UnityEditor.Handles.color = GetShapeColor(shape);

        var m = Matrix4x4.TRS(checkTransform.position, checkTransform.rotation, Vector3.one);
        using (new UnityEditor.Handles.DrawingScope(m))
        {
            switch (shape)
            {
                case AttackCheckShape.Point:
                    UnityEditor.Handles.DrawSolidDisc(Vector3.zero, Vector3.forward, 0.06f);
                    break;

                case AttackCheckShape.Circle:
                    UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.forward, Mathf.Max(0f, circleRadius));
                    break;

                case AttackCheckShape.Box:
                    UnityEditor.Handles.DrawWireCube(
                        Vector3.zero,
                        new Vector3(Mathf.Max(0, boxSize.x), Mathf.Max(0, boxSize.y), 0f));
                    break;

                case AttackCheckShape.Capsule:
                    {
                        var w = Mathf.Max(0, capsuleSize.x);
                        var h = Mathf.Max(0, capsuleSize.y);
                        DrawCapsule2D(w, h, capsuleDirection == CapsuleDirection.Horizontal);
                        break;
                    }

                case AttackCheckShape.Line:
                    {
                        var len = Mathf.Max(0, lineLength);
                        var half = len * 0.5f;
                        var p0 = new Vector3(-half, 0f, 0f);
                        var p1 = new Vector3(+half, 0f, 0f);
                        UnityEditor.Handles.DrawLine(p0, p1);

                        if (lineThickness > 0f)
                        {
                            float t = lineThickness * 0.5f;
                            UnityEditor.Handles.DrawLine(p0 + Vector3.up * t, p1 + Vector3.up * t);
                            UnityEditor.Handles.DrawLine(p0 - Vector3.up * t, p1 - Vector3.up * t);
                        }
                        break;
                    }

                case AttackCheckShape.Arc:
                    DrawArc2D(Mathf.Max(0, arcInnerRadius), Mathf.Max(0, arcRadius), arcAngleDegrees);
                    break;

                case AttackCheckShape.Polygon:
                    if (polygonPoints != null && polygonPoints.Count > 1)
                    {
                        for (int i = 0; i < polygonPoints.Count; i++)
                        {
                            var a = (Vector3)polygonPoints[i];
                            var b = (Vector3)polygonPoints[(i + 1) % polygonPoints.Count];
                            UnityEditor.Handles.DrawLine(a, b);
                        }
                    }
                    break;
            }
        }

        UnityEditor.Handles.color = prevColor;
    }

    private static Color GetShapeColor(AttackCheckShape s)
    {
        return s switch
        {
            AttackCheckShape.Point => new Color(1f, 1f, 1f, 0.95f),  // white
            AttackCheckShape.Circle => new Color(0.20f, 0.75f, 1f, 0.9f),   // cyan
            AttackCheckShape.Box => new Color(0.25f, 1f, 0.45f, 0.9f),   // green
            AttackCheckShape.Capsule => new Color(1f, 0.6f, 0.2f, 0.9f),     // orange
            AttackCheckShape.Line => new Color(0.95f, 0.95f, 0.2f, 0.9f), // yellow
            AttackCheckShape.Arc => new Color(1f, 0.35f, 0.6f, 0.9f),    // pink
            AttackCheckShape.Polygon => new Color(0.8f, 0.6f, 1f, 0.9f),     // purple
            _ => Color.white
        };
    }

    private static void DrawArc2D(float innerR, float outerR, float angleDeg)
    {
        int steps = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(angleDeg) / 6f));
        float step = angleDeg / steps;

        Vector3 prevOuter = Polar2D(outerR, 0);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 nextOuter = Polar2D(outerR, step * i);
            UnityEditor.Handles.DrawLine(prevOuter, nextOuter);
            prevOuter = nextOuter;
        }

        if (innerR > 0f)
        {
            Vector3 prevInner = Polar2D(innerR, 0);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 nextInner = Polar2D(innerR, step * i);
                UnityEditor.Handles.DrawLine(prevInner, nextInner);
                prevInner = nextInner;
            }

            UnityEditor.Handles.DrawLine(Polar2D(innerR, 0), Polar2D(outerR, 0));
            UnityEditor.Handles.DrawLine(Polar2D(innerR, angleDeg), Polar2D(outerR, angleDeg));
        }
        else
        {
            UnityEditor.Handles.DrawLine(Vector3.zero, Polar2D(outerR, 0));
            UnityEditor.Handles.DrawLine(Vector3.zero, Polar2D(outerR, angleDeg));
        }
    }

    private static void DrawCapsule2D(float width, float height, bool horizontal)
    {
        float r = horizontal ? height * 0.5f : width * 0.5f;
        float len = horizontal ? Mathf.Max(0, width - 2f * r)
                               : Mathf.Max(0, height - 2f * r);

        Vector3 a = horizontal ? new(-len * 0.5f, 0f, 0f) : new(0f, -len * 0.5f, 0f);
        Vector3 b = horizontal ? new(len * 0.5f, 0f, 0f) : new(0f, len * 0.5f, 0f);

        UnityEditor.Handles.DrawLine(a + (horizontal ? Vector3.up * r : Vector3.right * r),
                                     b + (horizontal ? Vector3.up * r : Vector3.right * r));
        UnityEditor.Handles.DrawLine(a - (horizontal ? Vector3.up * r : Vector3.right * r),
                                     b - (horizontal ? Vector3.up * r : Vector3.right * r));

        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
            float t0 = Mathf.PI * i / steps;
            float t1 = Mathf.PI * (i + 1) / steps;

            Vector3 p0 = a + new Vector3(Mathf.Cos(t0) * (horizontal ? r : 0f),
                                         Mathf.Sin(t0) * (horizontal ? r : r), 0f);
            Vector3 p1 = a + new Vector3(Mathf.Cos(t1) * (horizontal ? r : 0f),
                                         Mathf.Sin(t1) * (horizontal ? r : r), 0f);

            Vector3 q0 = b + new Vector3(Mathf.Cos(t0 + Mathf.PI) * (horizontal ? r : 0f),
                                         Mathf.Sin(t0 + Mathf.PI) * (horizontal ? r : r), 0f);
            Vector3 q1 = b + new Vector3(Mathf.Cos(t1 + Mathf.PI) * (horizontal ? r : 0f),
                                         Mathf.Sin(t1 + Mathf.PI) * (horizontal ? r : r), 0f);

            UnityEditor.Handles.DrawLine(p0, p1);
            UnityEditor.Handles.DrawLine(q0, q1);
        }
    }

    private static Vector3 Polar2D(float r, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
    }
#endif
}
