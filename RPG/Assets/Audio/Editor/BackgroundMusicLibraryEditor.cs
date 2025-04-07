using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BackgroundMusicLibrary))]
public class BackgroundMusicLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Background Music Library", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundName"), new GUIContent("Background Music Group Name"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("musicTracks"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
