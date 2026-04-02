#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

[CustomPropertyDrawer(typeof(AttackSpawnSpec))]
public class AttackSpawnSpecDrawer : PropertyDrawer
{
    private static readonly float Line = EditorGUIUtility.singleLineHeight;
    private const float Space = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EnsureDefaults(property);

        Rect foldoutRect = new Rect(position.x, position.y, position.width, Line);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = position.y + Line + Space;

        DrawProp(ref y, position, property, "name");
        DrawProp(ref y, position, property, "attackPoints");
        DrawProp(ref y, position, property, "prefab");
        DrawProp(ref y, position, property, "collisionShape");
        DrawProp(ref y, position, property, "explodePoints");
        DrawProp(ref y, position, property, "sounds");
        DrawProp(ref y, position, property, "control");

        DrawProp(ref y, position, property, "canMove");

        SerializedProperty canMove = property.FindPropertyRelative("canMove");
        if (canMove != null && canMove.boolValue)
        {
            DrawProp(ref y, position, property, "canParry");
            DrawProp(ref y, position, property, "speed");
            DrawProp(ref y, position, property, "movementType");

            SerializedProperty movementType = property.FindPropertyRelative("movementType");
            if (movementType != null)
            {
                SpecialMovementType moveType = (SpecialMovementType)movementType.enumValueIndex;

                switch (moveType)
                {
                    case SpecialMovementType.Homing:
                        DrawProp(ref y, position, property, "moveTimer");
                        DrawProp(ref y, position, property, "homingTurnSpeed");
                        DrawProp(ref y, position, property, "homingRecoverUpStrength");
                        break;

                    case SpecialMovementType.Dive:
                        DrawProp(ref y, position, property, "maxLoops");
                        DrawProp(ref y, position, property, "attackSpeedBoost");
                        DrawProp(ref y, position, property, "circleRadius");
                        break;
                }
            }
        }

        DrawProp(ref y, position, property, "explodes");

        SerializedProperty explodes = property.FindPropertyRelative("explodes");
        if (explodes != null && explodes.boolValue)
        {
            DrawProp(ref y, position, property, "lifeTimer");
            DrawProp(ref y, position, property, "destroyAfterTime");

            DrawProp(ref y, position, property, "canGrow");
            SerializedProperty canGrow = property.FindPropertyRelative("canGrow");
            if (canGrow != null && canGrow.boolValue)
            {
                DrawProp(ref y, position, property, "growSpeed");
                DrawProp(ref y, position, property, "maxSize");
            }

            DrawProp(ref y, position, property, "useExplosionTimer");
            SerializedProperty useExplosionTimer = property.FindPropertyRelative("useExplosionTimer");
            if (useExplosionTimer != null && useExplosionTimer.boolValue)
            {
                DrawProp(ref y, position, property, "explosionTimer");
            }

            DrawProp(ref y, position, property, "hasAnimation");
            SerializedProperty hasAnimation = property.FindPropertyRelative("hasAnimation");
            if (hasAnimation != null && hasAnimation.boolValue)
            {
                DrawProp(ref y, position, property, "animationType");
                DrawProp(ref y, position, property, "animationBoolName");
            }
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return Line;

        float height = Line + Space;

        height += GetPropHeight(property, "name");
        height += GetPropHeight(property, "attackPoints");
        height += GetPropHeight(property, "prefab");
        height += GetPropHeight(property, "collisionShape");
        height += GetPropHeight(property, "explodePoints");
        height += GetPropHeight(property, "sounds");
        height += GetPropHeight(property, "control");

        height += GetPropHeight(property, "canMove");

        SerializedProperty canMove = property.FindPropertyRelative("canMove");
        if (canMove != null && canMove.boolValue)
        {
            height += GetPropHeight(property, "canParry");
            height += GetPropHeight(property, "speed");
            height += GetPropHeight(property, "movementType");

            SerializedProperty movementType = property.FindPropertyRelative("movementType");
            if (movementType != null)
            {
                SpecialMovementType moveType = (SpecialMovementType)movementType.enumValueIndex;

                switch (moveType)
                {
                    case SpecialMovementType.Homing:
                        height += GetPropHeight(property, "moveTimer");
                        height += GetPropHeight(property, "homingTurnSpeed");
                        height += GetPropHeight(property, "homingRecoverUpStrength");
                        break;

                    case SpecialMovementType.Dive:
                        height += GetPropHeight(property, "maxLoops");
                        height += GetPropHeight(property, "attackSpeedBoost");
                        height += GetPropHeight(property, "circleRadius");
                        break;
                }
            }
        }

        height += GetPropHeight(property, "explodes");

        SerializedProperty explodes = property.FindPropertyRelative("explodes");
        if (explodes != null && explodes.boolValue)
        {
            height += GetPropHeight(property, "lifeTimer");
            height += GetPropHeight(property, "destroyAfterTime");

            height += GetPropHeight(property, "canGrow");
            SerializedProperty canGrow = property.FindPropertyRelative("canGrow");
            if (canGrow != null && canGrow.boolValue)
            {
                height += GetPropHeight(property, "growSpeed");
                height += GetPropHeight(property, "maxSize");
            }

            height += GetPropHeight(property, "useExplosionTimer");
            SerializedProperty useExplosionTimer = property.FindPropertyRelative("useExplosionTimer");
            if (useExplosionTimer != null && useExplosionTimer.boolValue)
            {
                height += GetPropHeight(property, "explosionTimer");
            }

            height += GetPropHeight(property, "hasAnimation");
            SerializedProperty hasAnimation = property.FindPropertyRelative("hasAnimation");
            if (hasAnimation != null && hasAnimation.boolValue)
            {
                height += GetPropHeight(property, "animationType");
                height += GetPropHeight(property, "animationBoolName");
            }
        }

        return height;
    }

    private void DrawProp(ref float y, Rect position, SerializedProperty root, string name)
    {
        SerializedProperty prop = root.FindPropertyRelative(name);
        if (prop == null)
            return;

        y += GetFieldSpace(name);

        float height = EditorGUI.GetPropertyHeight(prop, true);
        Rect rect = new Rect(position.x, y, position.width, height);
        EditorGUI.PropertyField(rect, prop, true);
        y += height + Space;
    }

    private float GetPropHeight(SerializedProperty root, string name)
    {
        SerializedProperty prop = root.FindPropertyRelative(name);
        if (prop == null)
            return 0f;

        return GetFieldSpace(name) + EditorGUI.GetPropertyHeight(prop, true) + Space;
    }

    private float GetFieldSpace(string fieldName)
    {
        FieldInfo field = typeof(AttackSpawnSpec).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
            return 0f;

        SpaceAttribute space = Attribute.GetCustomAttribute(field, typeof(SpaceAttribute)) as SpaceAttribute;
        if (space == null)
            return 0f;

        return space.height;
    }

    #region Defaults
    private void EnsureDefaults(SerializedProperty property)
    {
        SerializedProperty initProp = property.FindPropertyRelative("defaultsInitialized");
        if (initProp == null || initProp.boolValue)
            return;

        SetBool(property, "canMove", false);
        SetBool(property, "canParry", false);
        SetFloat(property, "speed", 5f);
        SetEnumIndex(property, "movementType", (int)SpecialMovementType.None);

        SetFloat(property, "moveTimer", 2f);
        SetFloat(property, "homingTurnSpeed", 4f);
        SetFloat(property, "homingRecoverUpStrength", 1.5f);

        SetInt(property, "maxLoops", 1);
        SetFloat(property, "attackSpeedBoost", 3f);
        SetFloat(property, "circleRadius", 2f);

        SetEnumIndex(property, "collisionShape", (int)SpecialCollisionShape.Circle);

        SetBool(property, "explodes", false);
        SetFloat(property, "lifeTimer", 3f);
        SetFloat(property, "destroyAfterTime", 3f);

        SetBool(property, "canGrow", false);
        SetFloat(property, "growSpeed", 1f);
        SetFloat(property, "maxSize", 1f);

        SetBool(property, "useExplosionTimer", false);
        SetFloat(property, "explosionTimer", 1.5f);

        SetBool(property, "hasAnimation", false);
        SetEnumIndex(property, "animationType", (int)SpecialAnimationType.None);

        initProp.boolValue = true;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetBool(SerializedProperty root, string name, bool value)
    {
        var prop = root.FindPropertyRelative(name);
        if (prop != null) prop.boolValue = value;
    }

    private static void SetFloat(SerializedProperty root, string name, float value)
    {
        var prop = root.FindPropertyRelative(name);
        if (prop != null) prop.floatValue = value;
    }

    private static void SetInt(SerializedProperty root, string name, int value)
    {
        var prop = root.FindPropertyRelative(name);
        if (prop != null) prop.intValue = value;
    }

    private static void SetEnumIndex(SerializedProperty root, string name, int value)
    {
        var prop = root.FindPropertyRelative(name);
        if (prop != null) prop.enumValueIndex = value;
    }
    #endregion
}
#endif