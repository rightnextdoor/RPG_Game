using System.Collections.Generic;
using UnityEngine;

public enum AbilityPreference
{
    Mixed,
    Long,
    Short
}

[System.Serializable]
public class AttackDetail
{
    #region Shared
    [Header("Shared")]
    public string name;
    public string animBoolName;
    public string stateName;
    public BattleAction action;
    public AbilityPreference preference = AbilityPreference.Mixed;
    public bool unlocked = true;
    public bool cannotUseWithTeleport = false;
    public bool canAttackAfterTeleport = false;
    public float chance = 1f;

    public float minCooldown = 0f;
    public float maxCooldown = 0f;
    public float startCooldown = 0f;

    public float rangeMin;
    public float rangeMax;

    #endregion

    #region Attack
    [Space(2)]
    [Header("Attack")]
    public float lingerTime = 0f;

    public int attackAmount;

    public float attackTimeMin;
    public float attackTimeMax;

    #endregion

    #region Evasion
    [Space(2)]
    [Header("Evasion")]
    public float evasionSpeedMultiplier = 1.80f;
    public float evasionDuration = 2.5f;

    public float evadeBackAwayMin = 5f;
    public float evadeBackAwayMax = 6.5f;
    public float evadePassPastMin = 5f;
    public float evadePassPastMax = 6.5f;
    public float evadeReducedFactor = 0.6f;
    public float evadeTinyRetreat = 0.35f;

    #endregion

    #region Stunned
    [Space(2)]
    [Header("Stunned Info")]
    public float stunDuration = 1f;
    public Vector2 stunDirection = new Vector2(10f, 12f);
    public bool canBeStunned;
    public GameObject counterImage;

    #endregion

    #region Jump
    [Space(2)]
    [Header("Jump")]
    public Vector2 jumpAbilityVelocity = new Vector2(20f, 12f);
    public bool jumpBack = true;

    #endregion

    #region Checks / Specs / Sounds
    [Space(2)]
    [Header("Checks / Specs / Sounds")]
    public List<AttackCheck> attackChecks;

    public List<AttackSpawnSpec> spawnSpec;
    public StateSound[] sounds;

    #endregion
}