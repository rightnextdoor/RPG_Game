#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackCheck), true)]
public class AttackCheckDrawer : PropertyDrawer
{
    const float VSP = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var y = position.y;
        var w = position.width;

        // Header
        var header = HeaderText(property);
        var headerRect = new Rect(position.x, y, w, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(headerRect, header, EditorStyles.boldLabel);
        y = headerRect.yMax + VSP;

        // Common fields
        y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("label"), "Label");
        y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("attackPoints"), "Attack Points");
        y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("checkTransform"), "Check Transform");

        var shapeProp = property.FindPropertyRelative("shape");
        y = DrawProp(new Rect(position.x, y, w, 0), shapeProp, "Shape");

        // Shape-specific
        var shape = (AttackCheckShape)shapeProp.enumValueIndex;
        switch (shape)
        {
            case AttackCheckShape.Point:
                // no size fields
                break;

            case AttackCheckShape.Circle:
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("circleRadius"), "Radius");
                break;

            case AttackCheckShape.Box:
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("boxSize"), "Size");
                break;

            case AttackCheckShape.Capsule:
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("capsuleSize"), "Size");
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("capsuleDirection"), "Direction");
                break;

            case AttackCheckShape.Line:
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("lineLength"), "Length");
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("lineThickness"), "Thickness");
                break;

            case AttackCheckShape.Arc:
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("arcRadius"), "Radius");
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("arcInnerRadius"), "Inner Radius");
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("arcAngleDegrees"), "Angle (deg)");
                break;

            case AttackCheckShape.Polygon:
                y = DrawProp(new Rect(position.x, y, w, 0), property.FindPropertyRelative("polygonPoints"), "Points");
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float total = 0f;

        // header
        total += EditorGUIUtility.singleLineHeight + VSP;

        // common fields
        total += PropHeight(property.FindPropertyRelative("label"));
        total += PropHeight(property.FindPropertyRelative("attackPoints"));
        total += PropHeight(property.FindPropertyRelative("checkTransform"));
        total += PropHeight(property.FindPropertyRelative("shape"));

        // shape-specific
        var shapeProp = property.FindPropertyRelative("shape");
        var shape = (AttackCheckShape)shapeProp.enumValueIndex;

        switch (shape)
        {
            case AttackCheckShape.Point:
                break;

            case AttackCheckShape.Circle:
                total += PropHeight(property.FindPropertyRelative("circleRadius"));
                break;

            case AttackCheckShape.Box:
                total += PropHeight(property.FindPropertyRelative("boxSize"));
                break;

            case AttackCheckShape.Capsule:
                total += PropHeight(property.FindPropertyRelative("capsuleSize"));
                total += PropHeight(property.FindPropertyRelative("capsuleDirection"));
                break;

            case AttackCheckShape.Line:
                total += PropHeight(property.FindPropertyRelative("lineLength"));
                total += PropHeight(property.FindPropertyRelative("lineThickness"));
                break;

            case AttackCheckShape.Arc:
                total += PropHeight(property.FindPropertyRelative("arcRadius"));
                total += PropHeight(property.FindPropertyRelative("arcInnerRadius"));
                total += PropHeight(property.FindPropertyRelative("arcAngleDegrees"));
                break;

            case AttackCheckShape.Polygon:
                total += PropHeight(property.FindPropertyRelative("polygonPoints"));
                break;
        }

        return total;
    }

    // Helpers --------------

    float DrawProp(Rect r, SerializedProperty p, string niceLabel)
    {
        if (p == null) return r.y;
        float h = EditorGUI.GetPropertyHeight(p, includeChildren: true);
        r.height = h;
        EditorGUI.PropertyField(r, p, new GUIContent(niceLabel), includeChildren: true);
        return r.y + h + VSP;
    }

    float PropHeight(SerializedProperty p)
    {
        if (p == null) return 0f;
        return EditorGUI.GetPropertyHeight(p, includeChildren: true) + VSP;
    }

    string HeaderText(SerializedProperty property)
    {
        var labelProp = property.FindPropertyRelative("label");
        string label = labelProp != null ? labelProp.stringValue : "";
        var shapeProp = property.FindPropertyRelative("shape");
        string shape = shapeProp != null ? ((AttackCheckShape)shapeProp.enumValueIndex).ToString() : "?";
        return string.IsNullOrEmpty(label) ? $"Check ({shape})" : $"{label} ({shape})";
    }
}
#endif
