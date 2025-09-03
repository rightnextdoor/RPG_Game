using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackDetail
{
    public string name;
    public string animBoolName;

    public float cooldown = 1f;
    public float lingerTime = 0f;

    public int attackAmount;

    public float attackTimeMin;
    public float attackTimeMax;

    public float rangeMin;
    public float rangeMax;

    public List<AttackCheck> attackChecks;

    public List<AttackSpawnSpec> spawnPrefab;
    public StateSound[] sounds;

    [HideInInspector] public float lastTimeAttack;
}
