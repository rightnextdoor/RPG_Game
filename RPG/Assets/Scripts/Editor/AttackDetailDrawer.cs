using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackDetail))]
public class AttackDetailDrawer : PropertyDrawer
{
    private const float Space = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            string.IsNullOrWhiteSpace(GetString(property, "name")) ? label : new GUIContent(GetString(property, "name")),
            true
        );

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = position.y + EditorGUIUtility.singleLineHeight + Space;
        float width = position.width;

        DrawHeader(ref y, position.x, width, "Shared");
        DrawField(ref y, position, property, "name");
        DrawField(ref y, position, property, "animBoolName");
        DrawField(ref y, position, property, "action");
        DrawField(ref y, position, property, "unlocked");
        DrawField(ref y, position, property, "cannotUseWithTeleport");
        DrawField(ref y, position, property, "canAttackAfterTeleport");
        DrawField(ref y, position, property, "chance");
        DrawField(ref y, position, property, "minCooldown");
        DrawField(ref y, position, property, "maxCooldown");
        DrawField(ref y, position, property, "rangeMin");
        DrawField(ref y, position, property, "rangeMax");

        var actionProp = property.FindPropertyRelative("action");
        BattleAction action = (BattleAction)actionProp.enumValueIndex;

        switch (action)
        {
            case BattleAction.Attack:
                DrawHeader(ref y, position.x, width, "Attack");
                DrawField(ref y, position, property, "lingerTime");
                DrawField(ref y, position, property, "attackAmount");
                DrawField(ref y, position, property, "attackTimeMin");
                DrawField(ref y, position, property, "attackTimeMax");

                DrawHeader(ref y, position.x, width, "Checks / Specs / Sounds");
                DrawField(ref y, position, property, "attackChecks", true);
                DrawField(ref y, position, property, "spawnSpec", true);
                DrawField(ref y, position, property, "sounds", true);
                break;

            case BattleAction.Evade:
                DrawHeader(ref y, position.x, width, "Evasion");
                DrawField(ref y, position, property, "evasionSpeedMultiplier");
                DrawField(ref y, position, property, "evasionDuration");
                DrawField(ref y, position, property, "evadeBackAwayMin");
                DrawField(ref y, position, property, "evadeBackAwayMax");
                DrawField(ref y, position, property, "evadePassPastMin");
                DrawField(ref y, position, property, "evadePassPastMax");
                DrawField(ref y, position, property, "evadeReducedFactor");
                DrawField(ref y, position, property, "evadeTinyRetreat");
                break;

            case BattleAction.Stunned:
                DrawHeader(ref y, position.x, width, "Stunned Info");
                DrawField(ref y, position, property, "stunDuration");
                DrawField(ref y, position, property, "stunDirection");
                DrawField(ref y, position, property, "canBeStunned");
                DrawField(ref y, position, property, "counterImage");
                break;

            case BattleAction.Jump:
                DrawHeader(ref y, position.x, width, "Jump");
                DrawField(ref y, position, property, "jumpAbilityVelocity");
                DrawField(ref y, position, property, "jumpBack");
                break;

            case BattleAction.Teleport:
            case BattleAction.Custom:
                DrawHeader(ref y, position.x, width, action.ToString());
                EditorGUI.HelpBox(
                    new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight * 1.5f),
                    "No action-specific fields added yet.",
                    MessageType.Info
                );
                y += EditorGUIUtility.singleLineHeight * 1.5f + Space;
                break;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return h;

        h += Space;

        // Shared header + 11 shared fields
        h += HeaderHeight();
        h += FieldHeight(property, "name");
        h += FieldHeight(property, "animBoolName");
        h += FieldHeight(property, "action");
        h += FieldHeight(property, "unlocked");
        h += FieldHeight(property, "cannotUseWithTeleport");
        h += FieldHeight(property, "canAttackAfterTeleport");
        h += FieldHeight(property, "chance");
        h += FieldHeight(property, "minCooldown");
        h += FieldHeight(property, "maxCooldown");
        h += FieldHeight(property, "rangeMin");
        h += FieldHeight(property, "rangeMax");

        var actionProp = property.FindPropertyRelative("action");
        BattleAction action = (BattleAction)actionProp.enumValueIndex;

        switch (action)
        {
            case BattleAction.Attack:
                h += HeaderHeight();
                h += FieldHeight(property, "lingerTime");
                h += FieldHeight(property, "attackAmount");
                h += FieldHeight(property, "attackTimeMin");
                h += FieldHeight(property, "attackTimeMax");

                h += HeaderHeight();
                h += FieldHeight(property, "attackChecks", true);
                h += FieldHeight(property, "spawnSpec", true);
                h += FieldHeight(property, "sounds", true);
                break;

            case BattleAction.Evade:
                h += HeaderHeight();
                h += FieldHeight(property, "evasionSpeedMultiplier");
                h += FieldHeight(property, "evasionDuration");
                h += FieldHeight(property, "evadeBackAwayMin");
                h += FieldHeight(property, "evadeBackAwayMax");
                h += FieldHeight(property, "evadePassPastMin");
                h += FieldHeight(property, "evadePassPastMax");
                h += FieldHeight(property, "evadeReducedFactor");
                h += FieldHeight(property, "evadeTinyRetreat");
                break;

            case BattleAction.Stunned:
                h += HeaderHeight();
                h += FieldHeight(property, "stunDuration");
                h += FieldHeight(property, "stunDirection");
                h += FieldHeight(property, "canBeStunned");
                h += FieldHeight(property, "counterImage");
                break;

            case BattleAction.Jump:
                h += HeaderHeight();
                h += FieldHeight(property, "jumpAbilityVelocity");
                h += FieldHeight(property, "jumpBack");
                break;

            case BattleAction.Teleport:
            case BattleAction.Custom:
                h += HeaderHeight();
                h += EditorGUIUtility.singleLineHeight * 1.5f + Space;
                break;
        }

        return h;
    }

    private static void DrawHeader(ref float y, float x, float width, string text)
    {
        y += Space;
        EditorGUI.LabelField(
            new Rect(x, y, width, EditorGUIUtility.singleLineHeight),
            text,
            EditorStyles.boldLabel
        );
        y += EditorGUIUtility.singleLineHeight + Space;
    }

    private static void DrawField(ref float y, Rect position, SerializedProperty root, string relativeName, bool includeChildren = false)
    {
        SerializedProperty prop = root.FindPropertyRelative(relativeName);
        if (prop == null) return;

        float height = EditorGUI.GetPropertyHeight(prop, includeChildren);
        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, height),
            prop,
            includeChildren
        );
        y += height + Space;
    }

    private static float FieldHeight(SerializedProperty root, string relativeName, bool includeChildren = false)
    {
        SerializedProperty prop = root.FindPropertyRelative(relativeName);
        if (prop == null) return 0f;
        return EditorGUI.GetPropertyHeight(prop, includeChildren) + Space;
    }

    private static float HeaderHeight()
    {
        return EditorGUIUtility.singleLineHeight + (Space * 2f);
    }

    private static string GetString(SerializedProperty root, string relativeName)
    {
        SerializedProperty prop = root.FindPropertyRelative(relativeName);
        return prop != null ? prop.stringValue : string.Empty;
    }
}