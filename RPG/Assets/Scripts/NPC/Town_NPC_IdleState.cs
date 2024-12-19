using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_NPC_Idle : NPCState
{
    private Town_NPC npc;
    public Town_NPC_Idle(NPC _npcBase, NPCStateMachine _stateMachine, string _animBoolName, Town_NPC npc) : base(_npcBase, _stateMachine, _animBoolName)
    {
        this.npc = npc;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = npc.idleTime;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0)
            stateMachine.ChangeState(npc.moveState);
    }
}
