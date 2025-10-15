using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Combat/Enemy Attack Profile", fileName = "EnemyAttackProfile")]
public class EnemyAttackProfile : ScriptableObject
{
    [Header("Prefab Binding")]
    [Tooltip("Drag the prefab asset for this enemy type (root object). The Enemy component may be on a child.")]
    public GameObject enemyPrefabAsset;

    [Tooltip("Optional: path to a specific child under the prefab root (e.g. \"BossRoot/Logic/Enemy_Boss\"). Leave empty to auto-pick the first Enemy component found.")]
    public string enemyChildPath = "";

    public Enemy GetEnemyPrototype()
    {
        if (!enemyPrefabAsset) return null;

        // If a specific child path is provided, try to locate it.
        if (!string.IsNullOrEmpty(enemyChildPath))
        {
            var child = FindChildByPath(enemyPrefabAsset.transform, enemyChildPath);
            if (child)
            {
                var e = child.GetComponent<Enemy>();
                if (e) return e;
            }
        }

        // Fallback: find the first Enemy (or subclass) anywhere under the prefab root.
        return enemyPrefabAsset.GetComponentsInChildren<Enemy>(true).FirstOrDefault();
    }

    public IReadOnlyList<AttackDetail> GetAttacks()
    {
        var enemy = GetEnemyPrototype();
        if (enemy != null && enemy.attackDetails != null)
            return enemy.attackDetails;
        return System.Array.Empty<AttackDetail>();
    }

    public AttackDetail FindAttack(string attackName)
    {
        if (string.IsNullOrEmpty(attackName)) return null;
        return GetAttacks().FirstOrDefault(a => a != null && a.name == attackName);
    }

    // --- helpers ---

    private static Transform FindChildByPath(Transform root, string path)
    {
        // Supports slash-separated relative paths under the prefab root.
        // Ignores empty segments and trims whitespace to be forgiving.
        if (!root || string.IsNullOrEmpty(path)) return null;
        var segments = path.Split('/');
        Transform current = root;
        foreach (var raw in segments)
        {
            var seg = raw.Trim();
            if (string.IsNullOrEmpty(seg)) continue;
            current = current.Find(seg);
            if (!current) return null;
        }
        return current;
    }
}
