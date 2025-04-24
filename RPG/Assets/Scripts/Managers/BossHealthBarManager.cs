using UnityEngine;

public class BossHealthBarManager : MonoBehaviour
{
    public static BossHealthBarManager instance;

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

    private UI_BossHealth CurrentBossHealthUI =>
        UIManager.instance != null ? UIManager.instance.GetUIBossHealth() : null;

    public void BossFightStart(Enemy_Boss boss)
    {
        CurrentBossHealthUI?.ShowBossBar(boss);
    }

    public void BossFightOver()
    {
        CurrentBossHealthUI?.HideBossBar();
    }
}
