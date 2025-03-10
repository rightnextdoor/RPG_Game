using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Skeleton : Enemy_Regular
{
    [Header("Multi Attack")]
    public bool HasMultiAttack;
    public bool IsSpearSkeleton;
    public float chanceToMultiAttack = 0;
    public float defaultChanceToMultiAttack = 5;
    [SerializeField] private float multiAttackCooldown = 5f;
    [HideInInspector] public float multiAttackCooldownTimer = 0;

    #region States
    public SkeletonIdleState idleState {  get; private set; }
    public SkeletonMoveState moveState { get; private set; }
    public SkeletonBattleState battleState { get; private set; }  
    public SkeletonAttackState attackState { get; private set; }
    public SkeletonAttackState attack2State { get; private set; }
    public SkeletonStunnedState stunnedState { get; private set; }
    public SkeletonDeadState deadState { get; private set; }
    public SkeletonEvasionState evasionState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new SkeletonIdleState(this, stateMachine, "Idle", this);
        moveState = new SkeletonMoveState(this, stateMachine, "Move", this);
        battleState = new SkeletonBattleState(this, stateMachine, "Battle", this);
        attackState = new SkeletonAttackState(this, stateMachine, "Attack", this);
        attack2State = new SkeletonAttackState(this, stateMachine, "Attack2", this);
        stunnedState = new SkeletonStunnedState(this, stateMachine, "Stunned", this);
        deadState = new SkeletonDeadState(this, stateMachine, "Die", this);
        evasionState = new SkeletonEvasionState(this, stateMachine, "Move", this);
    }

    protected override void Update()
    {
        base.Update();

        multiAttackCooldownTimer -= Time.deltaTime;
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    public override bool CanBeStunned()
    {
        if(base.CanBeStunned())
        {
            stateMachine.ChangeState(stunnedState);
            return true;
        }
        return false;
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new SkeletonDeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void SelfDestroy()
    {
        base.SelfDestroy();

        if (GetComponentInChildren<UI_HealthBar>() != null)
        {
            GetComponentInChildren<UI_HealthBar>().HidHealthBar();
        }    

        stateMachine.ChangeState(deadState);
        Destroy(gameObject, 2f);
    }

    public bool CanMultiAttack()
    {
        if (multiAttackCooldownTimer < 0)
        {
            if (Random.Range(0, 100) >= chanceToMultiAttack)
            {
                chanceToMultiAttack = defaultChanceToMultiAttack;
                multiAttackCooldownTimer = multiAttackCooldown;
                return true;
            }
            return false;
        }
        return false;
    }

    
}
