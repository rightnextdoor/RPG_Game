using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour, ISaveManager
{
    public static CheckpointManager instance { get; private set; }

    [SerializeField] private List<CheckpointData> checkpointList = new List<CheckpointData>();
    [SerializeField] private SceneField DefaultStartScene;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveCheckpoint(CheckpointData _checkpoint)
    {
        foreach (var check in checkpointList)
        {
            if (check == null) continue;

            if (check.isLastCheckpoint && check.checkpointId != _checkpoint.checkpointId)
                check.isLastCheckpoint = false;
        }

        _checkpoint.isActivated = true;
        _checkpoint.isLastCheckpoint = true;

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
            if(checkpoint == null) continue;

            checkpoint.isActivated = false;
            checkpoint.isLastCheckpoint = false;
        }

        SaveManager.instance.SaveGame();
    }

    public List<CheckpointData> GetActivatedCheckpoints()
    {
        return checkpointList.Where(c => c !=null && c.isActivated 
            && c.sceneName != SceneManager.GetActiveScene().name).ToList();
    }

    private IEnumerator SetPlayerPositionAfterSceneLoad(Vector3 position)
    {
        yield return new WaitUntil(() => PlayerManager.instance != null && PlayerManager.instance.player != null);
        PlayerManager.instance.player.transform.position = position;
    }

    public void SaveData(ref GameData _data)
    {
        _data.checkpoints.Clear();

        foreach (CheckpointData checkpoint in checkpointList)
        {
            if (checkpoint != null)
                _data.checkpoints.Add(checkpoint.checkpointId, checkpoint);
        }
    }

    public void LoadData(GameData _data)
    {
        if (_data.checkpoints == null || _data.checkpoints.Count == 0)
            return;

        foreach (var checkpoint in checkpointList)
        {
            if (checkpoint == null) continue;

            if (_data.checkpoints.TryGetValue(checkpoint.checkpointId, out var savedCheckpoint))
            {
                checkpoint.isActivated = savedCheckpoint.isActivated;
                checkpoint.isLastCheckpoint = savedCheckpoint.isLastCheckpoint;
            }
        }
    }


#if UNITY_EDITOR
    [ContextMenu("Fill up checkpoint data base")]
    private void FillUpItemDataBase() => checkpointList = new List<CheckpointData>(GetItemDataBase());

    private List<CheckpointData> GetItemDataBase()
    {
        List<CheckpointData> checkDataBase = new List<CheckpointData>();
        string[] assetName = AssetDatabase.FindAssets("", new[] { "Assets/Data/Checkpoints" });

        foreach (string SOName in assetName)
        {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<CheckpointData>(SOpath);
            checkDataBase.Add(itemData);
        }
        checkDataBase = checkDataBase.Where(c => c != null).ToList();
        return checkDataBase;
    }
#endif

}
