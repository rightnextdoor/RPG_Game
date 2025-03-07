using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Crab : Enemy_Regular
{
    [Header("Crab info")]
    public float hitTimer = 1f;
    #region States
    public CrabMoveState moveState { get; private set; }
    public CrabDeadState deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        moveState = new CrabMoveState(this, stateMachine, "Move", this);
        deadState = new CrabDeadState(this, stateMachine, "Idle", this);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(moveState);
    }

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }
}
