using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ZoneSceneMappingWizard : EditorWindow
{
    private string zoneName = "NewZone";
    private List<SceneAsset> sceneAssets = new List<SceneAsset>();

    [MenuItem("Tools/Audio/Zone Scene Mapping Wizard")]
    public static void ShowWindow()
    {
        GetWindow<ZoneSceneMappingWizard>("Zone Scene Wizard");
    }

    private Vector2 scroll;

    private void OnGUI()
    {
        GUILayout.Label("Zone Scene Mapping", EditorStyles.boldLabel);
        zoneName = EditorGUILayout.TextField("Zone Name", zoneName);

        EditorGUILayout.Space(10);
        GUILayout.Label("Scene Files", EditorStyles.boldLabel);

        // Custom drag-and-drop box
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag Scene Files Here", EditorStyles.helpBox);

        HandleSceneDragAndDrop(dropArea);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(200));
        for (int i = 0; i < sceneAssets.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sceneAssets[i] = (SceneAsset)EditorGUILayout.ObjectField($"Scene {i + 1}", sceneAssets[i], typeof(SceneAsset), false);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                sceneAssets.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Scene"))
        {
            sceneAssets.Add(null);
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Create Zone Mapping"))
        {
            CreateZoneMapping();
        }
    }

    private void HandleSceneDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is SceneAsset sceneAsset && !sceneAssets.Contains(sceneAsset))
                        {
                            sceneAssets.Add(sceneAsset);
                        }
                    }

                    evt.Use();
                }
            }
        }
    }

    private void CreateZoneMapping()
    {
        if (string.IsNullOrEmpty(zoneName))
        {
            Debug.LogWarning("Zone name is required.");
            return;
        }

        string folderPath = "Assets/Audio/SoundData/ZoneMappings";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        ZoneSceneMapping zoneAsset = ScriptableObject.CreateInstance<ZoneSceneMapping>();
        var newZone = new ZoneSceneMapping.ZoneDefinition
        {
            zoneName = zoneName,
            sceneNames = new List<string>()
        };

        foreach (var scene in sceneAssets)
        {
            if (scene != null)
            {
                string scenePath = AssetDatabase.GetAssetPath(scene);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                newZone.sceneNames.Add(sceneName);
            }
        }

        zoneAsset.zones.Add(newZone);

        string path = $"{folderPath}/{zoneName}.asset";
        AssetDatabase.CreateAsset(zoneAsset, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Zone Scene Mapping created at: {path}");

        zoneName = "NewZone";
        sceneAssets.Clear();
    }
}
