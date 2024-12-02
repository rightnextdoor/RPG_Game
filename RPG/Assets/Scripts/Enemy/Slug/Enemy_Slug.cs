using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Slug : Enemy_Regular
{
    [Header("Slug info")]
    public float hitTimer = 1f;
    #region States
    public SlugMoveState moveState { get; private set; }
    public SlugDeadState deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        moveState = new SlugMoveState(this, stateMachine, "Move", this);
        deadState = new SlugDeadState(this, stateMachine, "Idle", this);
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
