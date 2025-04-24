using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CheckpointManager : MonoBehaviour, ISaveManager
{
    public static CheckpointManager instance { get; private set; }

    [SerializeField] private List<CheckpointData> checkpointList = new List<CheckpointData>();
    [SerializeField] private SceneField DefaultStartScene;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-parent to GameManager if it exists
        GameObject gm = GameObject.Find("GameManager");
        if (gm != null) transform.SetParent(gm.transform);
    }

    public void SaveCheckpoint(CheckpointData checkpoint)
    {
        foreach (var check in checkpointList)
        {
            if (check == null) continue;

            if (check.isLastCheckpoint && check.checkpointId != checkpoint.checkpointId)
                check.isLastCheckpoint = false;
        }

        checkpoint.isActivated = true;
        checkpoint.isLastCheckpoint = true;

        SaveManager.instance.SaveGame();
    }

    public void ContinueGame()
    {
        if (checkpointList != null)
        {
            var lastCheckpoint = checkpointList
                .Where(c => c != null && c.isLastCheckpoint && c.isActivated)
                .FirstOrDefault();

            if (lastCheckpoint != null)
            {
                SceneManager.LoadScene(lastCheckpoint.sceneName);
                StartCoroutine(SetPlayerPositionAfterSceneLoad(lastCheckpoint.checkpointPosition));
                return;
            }
        }

        // No valid checkpoint: load default scene and spawn at start
        SceneManager.LoadScene(DefaultStartScene);       
        GameManager.instance.skipEntryCutscene = true;
        StartCoroutine(SetPlayerPositionAfterSceneLoad(Vector3.zero));
    }

    public void TeleportToCheckpoint(CheckpointData checkpoint)
    {
        if (!checkpoint.isActivated)
        {
            Debug.LogWarning("Checkpoint not activated!");
            return;
        }

        SaveCheckpoint(checkpoint);
        SceneManager.LoadScene(checkpoint.sceneName);
        GameManager.instance.skipEntryCutscene = true;
        StartCoroutine(SetPlayerPositionAfterSceneLoad(checkpoint.checkpointPosition));
    }

    public void ResetCheckpoints()
    {
        foreach (var checkpoint in checkpointList)
        {
            if (checkpoint == null) continue;

            checkpoint.isActivated = false;
            checkpoint.isLastCheckpoint = false;
        }

        SaveManager.instance.SaveGame();
    }

    public List<CheckpointData> GetActivatedCheckpoints()
    {
        return checkpointList
            .Where(c => c != null && c.isActivated && c.sceneName != SceneManager.GetActiveScene().name)
            .ToList();
    }

    private IEnumerator SetPlayerPositionAfterSceneLoad(Vector3 position)
    {
        yield return new WaitUntil(() => PlayerManager.instance != null && PlayerManager.instance.player != null);
        PlayerManager.instance.player.transform.position = position;
    }

    public void SaveData(ref GameData data)
    {
        data.checkpoints.Clear();

        foreach (var checkpoint in checkpointList)
        {
            if (checkpoint != null)
                data.checkpoints.Add(checkpoint.checkpointId, checkpoint);
        }
    }

    public void LoadData(GameData data)
    {
        if (data.checkpoints == null || data.checkpoints.Count == 0) return;

        foreach (var checkpoint in checkpointList)
        {
            if (checkpoint == null) continue;

            if (data.checkpoints.TryGetValue(checkpoint.checkpointId, out var savedCheckpoint))
            {
                checkpoint.isActivated = savedCheckpoint.isActivated;
                checkpoint.isLastCheckpoint = savedCheckpoint.isLastCheckpoint;
            }
        }
    }

#if UNITY_EDITOR
[ContextMenu("Fill up checkpoint database")]
private void FillUpCheckpointDatabase()
{
    var newCheckpoints = GetAllCheckpointAssets();

    foreach (var checkpoint in newCheckpoints)
    {
        if (checkpoint != null && !checkpointList.Contains(checkpoint))
        {
            checkpointList.Add(checkpoint);
        }
    }

    Debug.Log($"CheckpointManager: Added {newCheckpoints.Count} checkpoint(s) (filtered for null and duplicates).");
}

private List<CheckpointData> GetAllCheckpointAssets()
{
    List<CheckpointData> result = new List<CheckpointData>();
    string[] assetGUIDs = AssetDatabase.FindAssets("", new[] { "Assets/Data/Checkpoints" });

    foreach (string guid in assetGUIDs)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var asset = AssetDatabase.LoadAssetAtPath<CheckpointData>(path);
        if (asset != null)
        {
            result.Add(asset);
        }
    }

    return result;
}
#endif

}
