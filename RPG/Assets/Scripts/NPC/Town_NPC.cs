using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Town_NPC : NPC
{
    #region States
    public Town_NPC_Idle idleState { get; private set; }
    public Town_NPC_MoveState moveState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        idleState = new Town_NPC_Idle(this, stateMachine, "Idle", this);
        moveState = new Town_NPC_MoveState(this, stateMachine, "Move", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }
}
