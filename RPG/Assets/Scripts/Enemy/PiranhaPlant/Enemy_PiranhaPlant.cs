using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_PiranhaPlant : Enemy_Regular
{
    #region States
    public PiranhaPlantIdleState idleState { get; private set; }
    public PiranhaPlantBattleState battleState { get; private set; }
    public PiranhaPlantAttackState attackState { get; private set; }
    public PiranhaPlantDeadState deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        idleState = new PiranhaPlantIdleState(this, stateMachine, "Idle", this);
        battleState = new PiranhaPlantBattleState(this, stateMachine, "Idle", this);
        attackState = new PiranhaPlantAttackState(this, stateMachine, "Attack", this);
        deadState = new PiranhaPlantDeadState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }
}
