using System;
using UnityEngine;

public enum BattleAction 
{
    Attack,
    Evade,
    Teleport,
    Custom,
    Jump
}

[Serializable]
public class AbilityEntry
{
    public string name;
    public string animBoolName;
    public EnemyState state;
    public BattleAction action;
    public float minCooldown;
    public float maxCooldown;
    public float cooldown;       
    public float lastTimeUsed; 

    public float? rangeMin;
    public float? rangeMax;

    public bool unlocked = true;  
    public float chance = 1f;

    public int? attackAmount;   
    public float? attackTimeMin;  
    public float? attackTimeMax;

    public bool jumpBack = false;
    public Vector2 jumpVelocity;  
    public float jumpFallTime;

    public AbilityEntry(
        string name,
        string animBoolName,
        EnemyState state,
        float minCooldown,
        float maxCooldown,
        BattleAction action,
        float initialCooldown = 0f,
        float? rangeMin = null,
        float? rangeMax = null,
        bool unlocked = true,
        float chance = 1f,
        int? attackAmount = null,
        float? attackTimeMin = null,
        float? attackTimeMax = null,
        Vector2 jumpVelocity = default,   // (0,0) by default
        float jumpFallTime = 1f,
        bool jumpBack = false
    )
    {
        this.name = name;
        this.animBoolName = animBoolName;
        this.state = state;

        this.minCooldown = minCooldown;
        this.maxCooldown = maxCooldown;
        this.cooldown = initialCooldown;

        this.action = action;

        this.rangeMin = rangeMin;
        this.rangeMax = rangeMax;

        this.unlocked = unlocked;
        this.chance = chance;

        this.attackAmount = attackAmount;
        this.attackTimeMin = attackTimeMin;
        this.attackTimeMax = attackTimeMax;

        this.jumpVelocity = jumpVelocity;
        this.jumpFallTime = jumpFallTime;
        this.jumpBack = jumpBack;
    }
}
