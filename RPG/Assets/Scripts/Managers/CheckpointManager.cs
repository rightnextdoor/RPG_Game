using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour, ISaveManager
{
    public static CheckpointManager instance;

    [SerializeField] private List<CheckpointData> checkpoints;
    private List<CheckpointData> travelCheckpoints = new List<CheckpointData>();
    private string savedCheckpointId;

    public bool checkpointChangeScenes;
    public bool isTraveling;

    private Transform player;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        player = PlayerManager.instance.player.transform;
    }

    public List<CheckpointData> UI_TravelCheckpoints()
    {
        travelCheckpoints.Clear();
        foreach (CheckpointData item in checkpoints)
        {
            if (item.activatedCheckpoint)
            {
                if (!item.lastSavedCheckpoint)
                {
                    travelCheckpoints.Add(item);
                }
            }
        }
        travelCheckpoints.Reverse();
        return travelCheckpoints;
    }

    public void DefaultCheckpoint()
    {
        foreach (CheckpointData item in checkpoints)
        {
            item.activatedCheckpoint = false;
            item.lastSavedCheckpoint = false;
        }
    }

    public void ActivatedCheckpoint(CheckpointData checkpointData)
    {
        checkpointData.activatedCheckpoint = true;

        AnimateCheckpoint(checkpointData);
    }

    private static void AnimateCheckpoint(CheckpointData checkpointData)
    {
        Checkpoint[] check = FindObjectsOfType<Checkpoint>();
        for (int i = 0; i < check.Length; i++)
        {
            if (check[i].checkpointData.checkpointId == checkpointData.checkpointId)
                check[i].ActivateAnim();
        }
    }

    public void UpdateLastSaveCheckpoint(CheckpointData checkpointData)
    {
        ClearAllSaveCheckpoint();
        checkpointData.lastSavedCheckpoint = true;
    }

    public void TravelTo(CheckpointData _checkpoint)
    {
        UI_FadeScreen.instance.TravelTo(_checkpoint.sceneName);
    }

    public void ClearAllSaveCheckpoint()
    {
        foreach (CheckpointData checkpoint in checkpoints)
        {
            checkpoint.lastSavedCheckpoint = false;
        }
    }

    public void LoadData(GameData _data)
    {
        StartCoroutine(LoadWithDelay(_data));
    }

    private void LoadCheckpoints(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.checkpoints)
        {
            foreach (CheckpointData checkpoint in checkpoints)
            {
                if (checkpoint.checkpointId == pair.Key && pair.Value == true)
                    ActivatedCheckpoint(checkpoint);
            }
        }
        travelCheckpoints = _data.travelCheckpoints;
    }

    private IEnumerator LoadWithDelay(GameData _data)
    {
        yield return new WaitForSeconds(.1f);

        LoadCheckpoints(_data);
        LoadCheckpoint(_data);
    }

    private void LoadCheckpoint(GameData _data)
    {
        if (_data.savedCheckpointId == null)
            return;

        savedCheckpointId = _data.savedCheckpointId;

        foreach (CheckpointData checkpoint in checkpoints)
        {
            if (savedCheckpointId == checkpoint.checkpointId)
            {
                if (!_data.checkpointChangeScenes)
                {
                    //for development
                    if (SceneManager.GetActiveScene().name == checkpoint.sceneName)
                        player.position = checkpoint.position;
                    else
                        TravelTo(checkpoint);
                }
            }
        }
    }

    private CheckpointData GetLastSavedCheckpoint()
    {
        CheckpointData savedCheckpoint = null;

        foreach (var checkpoint in checkpoints)
        {

            if (checkpoint.lastSavedCheckpoint == true)
            {
                savedCheckpoint = checkpoint;
            }
        }

        return savedCheckpoint;
    }

    public void SaveData(ref GameData _data)
    {
        if (GetLastSavedCheckpoint() != null)
            _data.savedCheckpointId = GetLastSavedCheckpoint().checkpointId;

        _data.checkpoints.Clear();

        foreach (CheckpointData checkpoint in checkpoints)
        {
            _data.checkpoints.Add(checkpoint.checkpointId, checkpoint.activatedCheckpoint);
        }

        _data.checkpointChangeScenes = checkpointChangeScenes;
        _data.travelCheckpoints = travelCheckpoints;
        _data.isTraveling = isTraveling;
    }

}
