using UnityEngine;

public class UI_BossHealth : MonoBehaviour
{
    [SerializeField] private GameObject bossHealthBar; // Assign in Inspector

    public void ShowBossBar(Enemy_Boss boss)
    {
        if (bossHealthBar == null) return;

        bossHealthBar.SetActive(true);
        bossHealthBar.GetComponent<UI_BossHealthBar>()?.BossFightStart(boss);
    }

    public void HideBossBar()
    {
        if (bossHealthBar == null) return;

        bossHealthBar.GetComponent<UI_BossHealthBar>()?.BossFightOver();
        bossHealthBar.SetActive(false);
    }
}
