using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZoneMusicLibrary))]
public class ZoneMusicLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Zone Music Library", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("zoneName"), new GUIContent("Zone Name"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("musicTracks"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
