using System;
using UnityEngine;

public enum BattleAction
{
    None,
    Attack,
    Evade,
    Jump,
    Stunned,
    Teleport,
    Custom
}

[Serializable]
public class AbilityEntry
{
    #region Shared

    public string name;
    public string animBoolName;
    public EnemyState state;
    public BattleAction action;
    public AbilityPreference preference;

    public float minCooldown;
    public float maxCooldown;
    public float startCooldown;
    public float cooldown;
    public float lastTimeUsed;

    public float? rangeMin;
    public float? rangeMax;

    public bool unlocked = true;
    public float chance = 1f;
    public bool cannotUseWithTeleport = false;
    public bool canAttackAfterTeleport = false;

    public int repeatStreak = 0;

    #endregion

    #region Attack

    public float lingerTime = 0f;
    public int attackAmount = 0;
    public float attackTimeMin = 0f;
    public float attackTimeMax = 0f;

    #endregion

    #region Evasion

    public float evasionSpeedMultiplier = 1.25f;
    public float evasionDuration = 0.5f;

    public float evadeBackAwayMin = 1.2f;
    public float evadeBackAwayMax = 2.0f;
    public float evadePassPastMin = 0.6f;
    public float evadePassPastMax = 1.0f;
    public float evadeReducedFactor = 0.6f;
    public float evadeTinyRetreat = 0.35f;

    #endregion

    #region Stunned

    public float stunDuration = 1f;
    public Vector2 stunDirection = new Vector2(10f, 12f);
    public bool canBeStunned;
    public GameObject counterImage;

    #endregion

    #region Jump

    public Vector2 jumpAbilityVelocity = new Vector2(20f, 12f);
    public bool jumpBack = true;

    #endregion

    public AbilityEntry(
        string name,
        string animBoolName,
        EnemyState state,
        float minCooldown,
        float maxCooldown,
        float startCooldown,
        BattleAction action,
        AbilityPreference preference = AbilityPreference.Mixed,
        float initialCooldown = 0f,
        float? rangeMin = null,
        float? rangeMax = null,
        bool unlocked = true,
        float chance = 1f,
        bool cannotUseWithTeleport = false,
        bool canAttackAfterTeleport = false,
        int repeatStreak = 0,
        float lingerTime = 0f,
        int attackAmount = 0,
        float attackTimeMin = 0f,
        float attackTimeMax = 0f,
        float evasionSpeedMultiplier = 1.25f,
        float evasionDuration = 0.5f,
        float evadeBackAwayMin = 1.2f,
        float evadeBackAwayMax = 2.0f,
        float evadePassPastMin = 0.6f,
        float evadePassPastMax = 1.0f,
        float evadeReducedFactor = 0.6f,
        float evadeTinyRetreat = 0.35f,
        float stunDuration = 1f,
        Vector2 stunDirection = default,
        bool canBeStunned = false,
        GameObject counterImage = null,
        Vector2 jumpAbilityVelocity = default,
        bool jumpBack = true
    )
    {
        this.name = name;
        this.animBoolName = animBoolName;
        this.state = state;
        this.action = action;
        this.preference = preference;

        this.minCooldown = minCooldown;
        this.maxCooldown = maxCooldown;
        this.startCooldown = startCooldown;
        this.cooldown = initialCooldown;
        this.lastTimeUsed = 0f;

        this.rangeMin = rangeMin;
        this.rangeMax = rangeMax;

        this.unlocked = unlocked;
        this.chance = chance;
        this.cannotUseWithTeleport = cannotUseWithTeleport;
        this.canAttackAfterTeleport = canAttackAfterTeleport;
        this.repeatStreak = repeatStreak;

        this.lingerTime = lingerTime;
        this.attackAmount = attackAmount;
        this.attackTimeMin = attackTimeMin;
        this.attackTimeMax = attackTimeMax;

        this.evasionSpeedMultiplier = evasionSpeedMultiplier;
        this.evasionDuration = evasionDuration;
        this.evadeBackAwayMin = evadeBackAwayMin;
        this.evadeBackAwayMax = evadeBackAwayMax;
        this.evadePassPastMin = evadePassPastMin;
        this.evadePassPastMax = evadePassPastMax;
        this.evadeReducedFactor = evadeReducedFactor;
        this.evadeTinyRetreat = evadeTinyRetreat;

        this.stunDuration = stunDuration;
        this.stunDirection = stunDirection == default ? new Vector2(10f, 12f) : stunDirection;
        this.canBeStunned = canBeStunned;
        this.counterImage = counterImage;

        this.jumpAbilityVelocity = jumpAbilityVelocity == default ? new Vector2(20f, 12f) : jumpAbilityVelocity;
        this.jumpBack = jumpBack;
    }
}