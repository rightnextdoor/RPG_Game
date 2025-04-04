using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enemy_Boss;

public class Enemy_NightBorne : Enemy_Regular
{
    #region States
    public NightBorne_IdleState idleState { get; private set; }
    public NightBorne_MoveState moveState { get; private set; }
    public NightBorne_BattleState battleState { get; private set; }
    public NightBorne_AttackState attackState { get; private set; }
    public NightBorne_StunnedState stunnedState { get; private set; }
    public NightBorne_DeadState deadState { get; private set; }
    public NightBorne_EvasionState evasionState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new NightBorne_IdleState(this, stateMachine, "Idle", this);
        moveState = new NightBorne_MoveState(this, stateMachine, "Move", this);
        battleState = new NightBorne_BattleState(this, stateMachine, "Battle", this);
        attackState = new NightBorne_AttackState(this, stateMachine, "Attack", this);
        stunnedState = new NightBorne_StunnedState(this, stateMachine, "Stunned", this);
        deadState = new NightBorne_DeadState(this, stateMachine, "Die", this);
        evasionState = new NightBorne_EvasionState(this, stateMachine, "Move", this);
    }

    protected override void Update()
    {
        base.Update();
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
            deadState = new NightBorne_DeadState(this, stateMachine, "Idle", this);
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
}
