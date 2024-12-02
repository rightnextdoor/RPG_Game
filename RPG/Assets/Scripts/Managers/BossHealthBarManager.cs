using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealthBarManager : MonoBehaviour
{
    public static BossHealthBarManager instance;
    public GameObject bossHealthBar;
    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }
    
    public void BossFightStart(Enemy_Boss boss)
    {
        bossHealthBar.GetComponent<UI_BossHealthBar>().BossFightStart(boss);
    }

    public void BossFightOver()
    {
        bossHealthBar.GetComponent<UI_BossHealthBar>().BossFightOver();
    }
}
