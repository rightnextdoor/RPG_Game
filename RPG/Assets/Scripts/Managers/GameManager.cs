using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, ISaveManager
{
    public static GameManager instance;

    private Transform player;

    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] private string closestCheckpointId;
    
    private Enemy_Boss[] getBosses;
    private SerializableDictionary<string, bool> saveBosses;
    
    private bool once;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        checkpoints = FindObjectsOfType<Checkpoint>();

        player = PlayerManager.instance.player.transform;

        getBosses = FindObjectsOfType<Enemy_Boss>();
        saveBosses = new SerializableDictionary<string, bool>();
        once = true;
    }
    private void Update()
    {
        if (once)
            UpdateBosses();
    }

    public void RestartScene()
    {
        SaveManager.instance.SaveGame();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void ClearAllSaveCheckpoint()
    {
        foreach (Checkpoint checkpoint in checkpoints)
        {
            checkpoint.lastSavedCheckpoint = false;
        }
    }

    public void UpdateBosses()
    {
        once = false;
        saveBosses.Clear();
        foreach (Enemy_Boss _boss in getBosses)
        {
            saveBosses.Add(_boss.bossId, _boss.bossIsDefeated);
        }
    }

    public void LoadData(GameData _data) 
    {
        LoadBosses(_data);
        StartCoroutine(LoadWithDelay(_data));
    }

    private void LoadCheckpoints(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.checkpoints)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.id == pair.Key && pair.Value == true)
                    checkpoint.ActivatedCheckpoint();
            }
        }
    }

    private void LoadBosses(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.bosses)
        {
            foreach (Enemy_Boss boss in getBosses)
            {
                if (boss.bossId == pair.Key)
                    boss.bossIsDefeated = pair.Value;
            }
        }
    }

    private IEnumerator LoadWithDelay(GameData _data)
    {
        yield return new WaitForSeconds(.1f);

        LoadCheckpoints(_data);
        LoadCheckpoint(_data);        
    }

    public void SaveData(ref GameData _data)
    {
        if(GetLastSavedCheckpoint() != null)
            _data.savedCheckpointId = GetLastSavedCheckpoint().id;

        _data.checkpoints.Clear();

        foreach (Checkpoint checkpoint in checkpoints)
        {
            _data.checkpoints.Add(checkpoint.id, checkpoint.activatedCheckpoint);
        }

        _data.bosses.Clear();
        foreach (KeyValuePair<string, bool> pair in saveBosses)
        {
            _data.bosses.Add(pair.Key, pair.Value);
        }
    }

    private void LoadCheckpoint(GameData _data)
    {
        if (_data.savedCheckpointId == null)
            return;

        closestCheckpointId = _data.savedCheckpointId;

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (closestCheckpointId == checkpoint.id)
                player.position = checkpoint.transform.position;
        }
    }

    private Checkpoint GetLastSavedCheckpoint()
    {
        Checkpoint savedCheckpoint = null;

        foreach (var checkpoint in checkpoints)
        {

            if (checkpoint.lastSavedCheckpoint == true)
            {
                savedCheckpoint = checkpoint;
            }
        }

        return savedCheckpoint;
    }

    public void PauseGame(bool _pause)
    {
        if (_pause)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
}
