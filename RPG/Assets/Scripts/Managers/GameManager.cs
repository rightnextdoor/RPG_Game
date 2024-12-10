using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, ISaveManager
{
    public static GameManager instance;

    [SerializeField] GameObject gameOverUI;

    private Enemy_Boss[] getBosses;
    private SerializableDictionary<string, bool> saveBosses;
 

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        getBosses = FindObjectsOfType<Enemy_Boss>();
        saveBosses = new SerializableDictionary<string, bool>();

        StartCoroutine(loadUpdateBossesWithDelay());
    }

    private IEnumerator loadUpdateBossesWithDelay()
    {
        yield return new WaitForSeconds(.1f);
        UpdateBosses();
    }

    public void RestartScene()
    {
        SaveManager.instance.SaveGame();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void GameOver()
    {
        AudioManager.instance.PlaySFX("GameOver", null);
        gameOverUI.SetActive(true);
    }

    public void UpdateBosses()
    {
        saveBosses.Clear();
        foreach (Enemy_Boss _boss in getBosses)
        {
            saveBosses.Add(_boss.bossId, _boss.bossIsDefeated);
        }
    }

    public void LoadData(GameData _data)
    {
        LoadBosses(_data);
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

    public void SaveData(ref GameData _data)
    {
        _data.bosses.Clear();
        foreach (KeyValuePair<string, bool> pair in saveBosses)
        {
            _data.bosses.Add(pair.Key, pair.Value);
        }
    }  

    public void PauseGame(bool _pause)
    {
        if (_pause)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
}
