using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Enemy_Black_Knight : Enemy_Boss
{
    #region States
    public Black_KnightIdleState idleState { get; private set; }
    public Black_KnightBattleState battleState { get; private set; }
    public Black_KnightAttackState attackState { get; private set; }
    public Black_KnightDeadState deadState { get; private set; }
    public Black_KnightStartState startState { get; private set; }

    #endregion
    protected override void Awake()
    {
        base.Awake();
        SetupDefaultFacingDir(-1);

        idleState = new Black_KnightIdleState(this, stateMachine, "Idle", this);
        battleState = new Black_KnightBattleState(this, stateMachine, "Battle", this);
        attackState = new Black_KnightAttackState(this, stateMachine, "Attack", this);
        deadState = new Black_KnightDeadState(this, stateMachine, "Die", this);
        startState = new Black_KnightStartState(this, stateMachine, "Idle", this);
    }
    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
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
}
