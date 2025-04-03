using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Wizard_Male : Enemy_Regular
{
    public List<EnemyState> attackStates {  get; private set; }

    #region States
    public Wizard_Male_IdleState idleState { get; private set; }
    public Wizard_Male_MoveState moveState { get; private set; }
    public Wizard_Male_BattleState battleState { get; private set; }
    public Wizard_Male_AttackState attackState { get; private set; }
    public Wizard_Male_AttackState attack2State { get; private set; }
    public Wizard_Male_StunnedState stunnedState { get; private set; }
    public Wizard_Male_DeadState deadState { get; private set; }
    public Wizard_Male_EvasionState evasionState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        
        attackStates = new List<EnemyState>();

        idleState = new Wizard_Male_IdleState(this, stateMachine, "Idle", this);
        moveState = new Wizard_Male_MoveState(this, stateMachine, "Move", this);
        battleState = new Wizard_Male_BattleState(this, stateMachine, "Battle", this);
        attackState = new Wizard_Male_AttackState(this, stateMachine, "Attack", this);
        attack2State = new Wizard_Male_AttackState(this, stateMachine, "Attack2", this);
        stunnedState = new Wizard_Male_StunnedState(this, stateMachine, "Stunned", this);
        deadState = new Wizard_Male_DeadState(this, stateMachine, "Die", this);
        evasionState = new Wizard_Male_EvasionState(this, stateMachine, "Move", this);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        AttackStates();
    }

    private void AttackStates()
    {
        attackStates.Clear();
        attackStates.Add(attackState);
        attackStates.Add(attack2State);
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
            deadState = new Wizard_Male_DeadState(this, stateMachine, "Idle", this);
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
