using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [HideInInspector] public bool skipEntryCutscene = false;
    public SceneField mainMenu;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        GameObject master = GameObject.Find("GameManager");
        if (master == null)
        {
            master = new GameObject("GameManager");
            DontDestroyOnLoad(master);
        }

        transform.SetParent(master.transform);
        DontDestroyOnLoad(master);
    }

    public void RestartScene()
    {
        EnemyManager.instance?.ResetEnemyDeath();      
        skipEntryCutscene = true;
        CheckpointManager.instance?.ContinueGame();
    }

    public void GameOver()
    {
        AudioManager.instance?.PlaySFX("GameOver");
        UIManager.instance?.GetUIGameOver()?.ShowGameOverScreen();
    }

    public void PauseGame(bool pause)
    {
        Time.timeScale = pause ? 0 : 1;
    }
}
