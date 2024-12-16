using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;


#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Checkpoint Data", menuName = "Data/Checkpoint")]
public class CheckpointData : ScriptableObject
{
    public string checkpointId;
    public string checkpointName;
    public bool activatedCheckpoint;
    public bool lastSavedCheckpoint;
    public SceneField sceneName;
    public Vector3 position;

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        checkpointId = AssetDatabase.AssetPathToGUID(path);
#endif
    }
}
