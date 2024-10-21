using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Enemy_Black_Knight : Enemy_Boss
{
    [Header("Black Knight info")]

    [Header("Summon")]
    [SerializeField] private GameObject summonController;
    [SerializeField] private int amountOfSummons = 3;
    [SerializeField] private float summonCooldown = 1f;
    private float lastTimeSummon;

    #region States
    public Black_KnightIdleState idleState { get; private set; }
    public Black_KnightBattleState battleState { get; private set; }
    public Black_KnightAttackState attackState { get; private set; }
    public Black_KnightDeadState deadState { get; private set; }
    public Black_KnightStartState startState { get; private set; }
    public Black_KnightSummonState summonState { get; private set; }

    #endregion

    [HideInInspector] public bool restartCooldown;
    private bool killSpawnOnce;

    protected override void Awake()
    {
        base.Awake();
        SetupDefaultFacingDir(-1);

        idleState = new Black_KnightIdleState(this, stateMachine, "Idle", this);
        battleState = new Black_KnightBattleState(this, stateMachine, "Battle", this);
        attackState = new Black_KnightAttackState(this, stateMachine, "Attack", this);
        deadState = new Black_KnightDeadState(this, stateMachine, "Die", this);
        startState = new Black_KnightStartState(this, stateMachine, "Idle", this);
        summonState = new Black_KnightSummonState(this, stateMachine, "Summon", this);
    }
    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
        killSpawnOnce = true;
    }

    protected override void Update()
    {
        base.Update();

        if(restartCooldown)
            SpawnCooldown();

        if(bossIsDefeated)
            KillSpawnEnemies();
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new Black_KnightDeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    private void SpawnCooldown()
    {
        int enemyCount = arena.GetComponent<EnemyZone>().EnemyCount;
        if (enemyCount <= 0)
        {
            lastTimeSummon = Time.time;
            restartCooldown = false;
        }
    }

    public void SummonSkeleton()
    {
        summonController.GetComponent<Black_Knight_Summon_Controller>().SetUpSummon(amountOfSummons);
    }

    public bool IsSummonOver()
    {
        return summonController.GetComponent<Black_Knight_Summon_Controller>().IsSummonOver();
    }

    public bool CanSummonSkeleton()
    {
        int enemyCount = arena.GetComponent<EnemyZone>().EnemyCount;

        if (Time.time >= lastTimeSummon + summonCooldown && enemyCount <= 0)
            return true;
        return false;
    }

    public override void KillSpawnEnemies()
    {
        base.KillSpawnEnemies();

        if (killSpawnOnce)
        {
            arena.GetComponent<EnemyZone>().KillSpawnEnemies();
            killSpawnOnce = false;
        }
    }

}
