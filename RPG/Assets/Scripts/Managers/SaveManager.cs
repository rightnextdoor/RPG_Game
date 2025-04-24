using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [Header("Save Settings")]
    [SerializeField] private string fileName = "game_save";
    //[SerializeField] private string filePath = "idbfs/dfdfdf549645erhh48fh"; // For web
    [SerializeField] private bool encryptData = false;

    private GameData gameData;
    private FileDataHandler dataHandler;
    private List<ISaveManager> saveManagers;

    [ContextMenu("Delete save file")]
    public void DeleteSavedData()
    {
        if (dataHandler == null)
            dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);

        //dataHandler = new FileDataHandler(filePath, fileName, encryptData); // for web
        dataHandler.Delete();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-parent to "GameManager" if it exists
        GameObject gm = GameObject.Find("GameManager");
        if (gm != null) transform.SetParent(gm.transform);

        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        //dataHandler = new FileDataHandler(filePath, fileName, encryptData); // for web

        saveManagers = FindAllSaveManagers();
        LoadGame();
    }

    public void NewGame()
    {
        Debug.Log("No save data");
        DefaultStat();
        gameData = new GameData();
    }

    private static void DefaultStat()
    {
        CheckpointManager.instance?.ResetCheckpoints();
        EnemyManager.instance?.DefaultStat();
        GateManager.instance?.DefaultGates();
        SkillManager.instance?.LockSkills();
    }

    public void LoadGame()
    {
        gameData = dataHandler.Load();

        if (gameData == null)
        {
            Debug.Log("No saved data found! Creating new game.");
            NewGame();
        }

        foreach (ISaveManager saveManager in saveManagers)
        {
            try
            {
                saveManager?.LoadData(gameData);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"LoadData failed for {saveManager?.GetType().Name}: {e.Message}");
            }
        }
    }

    public void SaveGame()
    {
        foreach (ISaveManager saveManager in saveManagers)
        {
            try
            {
                if (saveManager != null && saveManager.IsReady())
                    saveManager.SaveData(ref gameData);
            }
            catch (System.Exception)
            {
                //Debug.LogWarning($"SaveData failed for {saveManager?.GetType().Name}: {e.Message}");
            }
        }

        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit()
    {
        if (gameData != null)
        {
            try
            {
                SaveGame();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Save failed on quit: {e.Message}");
            }
        }
    }

    private List<ISaveManager> FindAllSaveManagers()
    {
        return FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveManager>().ToList();
    }

    public bool HasSavedData()
    {
        return dataHandler.Load() != null;
    }
}
