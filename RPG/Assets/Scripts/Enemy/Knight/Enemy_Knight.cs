using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KnightType
{
    Female,
    Male
}

public class Enemy_Knight : Enemy
{
    [Header("Knight info")]
    public KnightType knightType;
    public float blockDuration = .2f;

    #region States
    public KnightIdleState idleState { get; private set; }
    public KnightMoveState moveState { get; private set; }
    public KnightBattleState battleState { get; private set; }
    public KnightAttackState attackState { get; private set; }
    public KnightStunnedState stunnedState { get; private set; }
    public KnightDeadState deadState { get; private set; }
    public KnightBlockState blockState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new KnightIdleState(this, stateMachine, "Idle", this);
        moveState = new KnightMoveState(this, stateMachine, "Move", this);
        battleState = new KnightBattleState(this, stateMachine, "Battle", this);
        attackState = new KnightAttackState(this, stateMachine, "Attack", this);
        stunnedState = new KnightStunnedState(this, stateMachine, "Stunned", this);
        deadState = new KnightDeadState(this, stateMachine, "Die", this);
        blockState = new KnightBlockState(this, stateMachine, "Block", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    public override bool CanBeStunned()
    {
        if (base.CanBeStunned())
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
            deadState = new KnightDeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }
}
