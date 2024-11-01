using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Wizard : Enemy_Boss
{
    #region States
    public Wizard_IdleState idleState { get; private set; }
    public Wizard_BattleState battleState { get; private set; }
    public Wizard_AttackState attackState { get; private set; }
    public Wizard_DeadState deadState { get; private set; }
    public Wizard_StartState startState { get; private set; }
    public Wizard_MoveState moveState { get; private set; }
    public Wizard_TeleportState teleportState { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new Wizard_IdleState(this, stateMachine, "Idle", this);
        battleState = new Wizard_BattleState(this, stateMachine, "Battle", this);
        attackState = new Wizard_AttackState(this, stateMachine, "Attack", this);
        deadState = new Wizard_DeadState(this, stateMachine, "Die", this);
        startState = new Wizard_StartState(this, stateMachine, "Idle", this);
        moveState = new Wizard_MoveState(this, stateMachine, "Move", this);
        teleportState = new Wizard_TeleportState(this, stateMachine, "Teleport", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new Wizard_DeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }
}
