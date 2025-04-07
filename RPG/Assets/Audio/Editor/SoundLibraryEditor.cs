using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundLibrary))]
public class SoundLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Sound FX Library", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("soundName"), new GUIContent("Sound FX Group Name"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("sounds"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
