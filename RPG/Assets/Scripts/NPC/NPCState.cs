using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCState 
{
    protected NPCStateMachine stateMachine;
    protected NPC npcBase;
    protected Rigidbody2D rb;

    private string animBoolName;

    protected float stateTimer;
    protected bool triggerCalled;

    public NPCState(NPC _npcBase, NPCStateMachine _stateMachine, string _animBoolName)
    {
        this.npcBase = _npcBase;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
    }

    public virtual void Enter()
    {
        triggerCalled = false;
        rb = npcBase.rb;
        npcBase.anim.SetBool(animBoolName, true);
    }

    public virtual void Exit()
    {
        npcBase.anim.SetBool(animBoolName, false);
        npcBase.AssignLastAnimName(animBoolName);
    }

    public virtual void AnimationFinishTrigger() => triggerCalled = true;
}
