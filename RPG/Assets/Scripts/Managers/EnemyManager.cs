using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EnemyManager : MonoBehaviour, ISaveManager
{
    public static EnemyManager instance;

    [Header("Enemy Databases")]
    [SerializeField] private List<EnemyData_Boss> bossDataBase;
    [SerializeField] private List<EnemyData_Regular> enemyDataBase;

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

    #region Public Methods

    public void UpdateBosses()
    {
        SaveManager.instance.SaveGame();
    }

    public void DefaultStat()
    {
        foreach (var boss in bossDataBase)
        {
            if (boss != null)
            {
                boss.isDead = false;
            }
        }

        ResetEnemyDeath();
    }

    public void ResetEnemyDeath()
    {
        foreach (var enemy in enemyDataBase)
        {
            if (enemy != null)
            {
                enemy.isDead = false;
            }
        }
    }

    #endregion

    #region Save/Load

    public void LoadData(GameData _data)
    {
        foreach (var pair in _data.bosses)
        {
            foreach (var boss in bossDataBase)
            {
                if (boss != null && boss.enemyId == pair.Key)
                {
                    boss.isDead = pair.Value;
                    break;
                }
            }
        }

        foreach (var pair in _data.enemies)
        {
            foreach (var enemy in enemyDataBase)
            {
                if (enemy != null && enemy.enemyId == pair.Key)
                {
                    enemy.isDead = pair.Value;
                    break;
                }
            }
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.bosses.Clear();
        foreach (var boss in bossDataBase)
        {
            if (boss != null)
                _data.bosses.Add(boss.enemyId, boss.isDead);
        }

        _data.enemies.Clear();
        foreach (var enemy in enemyDataBase)
        {
            if (enemy != null)
                _data.enemies.Add(enemy.enemyId, enemy.isDead);
        }
    }

    #endregion

#if UNITY_EDITOR
    #region Editor Helpers

    [ContextMenu("Fill Enemy Database")]
    private void FillUpEnemyDatabase()
    {
        bossDataBase = GetEnemyDataFromPath<EnemyData_Boss>("Assets/Data/Enemy/Boss");
        enemyDataBase = GetEnemyDataFromPath<EnemyData_Regular>("Assets/Data/Enemy/Regular");
    }

    private List<T> GetEnemyDataFromPath<T>(string path) where T : ScriptableObject
    {
        List<T> results = new List<T>();
        string[] guids = AssetDatabase.FindAssets("", new[] { path });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                results.Add(asset);
        }

        return results;
    }

    #endregion
#endif
}
