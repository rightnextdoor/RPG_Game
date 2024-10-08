using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Crab : Enemy
{
    [Header("Crab info")]
    public float hitTimer = 1f;
    [HideInInspector] public float hitCountdown;
    #region States
    public CrabIdleState idleState { get; private set; }
    public CrabMoveState moveState { get; private set; }
    public CrabDeadState deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        idleState = new CrabIdleState(this, stateMachine, "Idle", this);
        moveState = new CrabMoveState(this, stateMachine, "Move", this);
        deadState = new CrabDeadState(this, stateMachine, "Idle", this);
    }

    protected override void Update()
    {
        base.Update();
        hitCountdown -= Time.deltaTime; 
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
