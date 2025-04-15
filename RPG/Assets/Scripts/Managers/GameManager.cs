using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] GameObject gameOverUI;

    public bool skipEntryCutscene = false; //Add this flag

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void RestartScene()
    {
        EnemyManager.instance.ResetEnemyDeath();

        //Set the flag before continuing
        skipEntryCutscene = true;

        CheckpointManager.instance.ContinueGame();
    }

    public void GameOver()
    {
        AudioManager.instance.PlaySFX("GameOver");
        gameOverUI.SetActive(true);
    }

    public void PauseGame(bool _pause)
    {
        Time.timeScale = _pause ? 0 : 1;
    }
}
