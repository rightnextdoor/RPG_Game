using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyManager : MonoBehaviour, ISaveManager
{
    public static EnemyManager instance;

    [SerializeField]private List<EnemyData_Boss> bossDataBase;
    [SerializeField]private List<EnemyData_Regular> enemyDataBase;


    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
    }

    public void UpdateBosses()
    {
        SaveManager.instance.SaveGame();
    }

    public void DefaultStat()
    {
        foreach (EnemyData_Boss boss in bossDataBase)
        {
            if(boss != null) 
                boss.isDead = false;
        }

        ResetEnemyDeath();
    }

    public void ResetEnemyDeath()
    {
        foreach (EnemyData_Regular enemy in enemyDataBase)
        {
            if(enemy != null)
                enemy.isDead = false;
        }
    }

    public void LoadData(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.bosses)
        {
            foreach (EnemyData_Boss boss in bossDataBase)
            {
                if (boss == null)
                    return;

                if (boss.enemyId == pair.Key)
                    boss.isDead = pair.Value;
            }
        }

        foreach (KeyValuePair<string, bool> pair in _data.enemies)
        {
            foreach (EnemyData_Regular enemy in enemyDataBase)
            {
                if (enemy == null)
                    return;

                if (enemy.enemyId == pair.Key)
                    enemy.isDead = pair.Value;
            }
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.bosses.Clear();
        foreach (EnemyData_Boss boss in bossDataBase)
        {
            if(boss != null)
                _data.bosses.Add(boss.enemyId, boss.isDead);
        }

        _data.enemies.Clear();
        foreach (EnemyData_Regular enemy in enemyDataBase)
        {
            if(enemy != null)
                _data.enemies.Add(enemy.enemyId, enemy.isDead);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Fill up Enemy data base")]
    private void FillUpBossDataBase()
    {
        bossDataBase = new List<EnemyData_Boss>(GetBossDataBase());
        enemyDataBase = new List<EnemyData_Regular>(GetEnemyDataBase());
    }

    private List<EnemyData_Boss> GetBossDataBase()
    {
        List<EnemyData_Boss> checkDataBase = new List<EnemyData_Boss>();
        string[] assetName = AssetDatabase.FindAssets("", new[] { "Assets/Data/Enemy/Boss" });

        foreach (string SOName in assetName)
        {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<EnemyData_Boss>(SOpath);
            checkDataBase.Add(itemData);
        }

        return checkDataBase;
    }

    private List<EnemyData_Regular> GetEnemyDataBase()
    {
        List<EnemyData_Regular> checkDataBase = new List<EnemyData_Regular>();
        string[] assetName = AssetDatabase.FindAssets("", new[] { "Assets/Data/Enemy/Regular" });

        foreach (string SOName in assetName)
        {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<EnemyData_Regular>(SOpath);
            checkDataBase.Add(itemData);
        }

        return checkDataBase;
    }
#endif
}
