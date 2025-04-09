using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Checkpoint Data", menuName = "Data/Checkpoint")]
public class CheckpointData : ScriptableObject
{
    [Header("ID")]
    public string checkpointId;
    public string checkpointName;

    [Header("Status")]
    public bool isActivated;
    public bool isLastCheckpoint;

    [Header("Scene Info")]
    public SceneField sceneName;

    [HideInInspector]
    public Vector3 checkpointPosition; // Hidden in default inspector

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        checkpointId = AssetDatabase.AssetPathToGUID(path);
#endif
    }
}
