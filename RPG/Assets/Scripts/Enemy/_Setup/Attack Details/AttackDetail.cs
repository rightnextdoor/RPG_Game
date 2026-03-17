using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackDetail
{
    public string name;
    public string animBoolName;
    public BattleAction action;
    public bool unlocked = true;
    public bool cannotUseWithTeleport = false;
    public float chance = 1f;

    public float minCooldown = 0f;
    public float maxCooldown = 0f;
    public float lingerTime = 0f;

    public int attackAmount;

    public float attackTimeMin;
    public float attackTimeMax;

    public float rangeMin;
    public float rangeMax;

    public List<AttackCheck> attackChecks;

    public List<AttackSpawnSpec> spawnSpec;
    public StateSound[] sounds;
}
