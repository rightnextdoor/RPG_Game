using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyData : ScriptableObject
{
    public string enemyId;
    public bool isDead;

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        enemyId = AssetDatabase.AssetPathToGUID(path);
#endif
    }
}
