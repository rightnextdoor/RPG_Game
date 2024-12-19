using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_NPC_MoveState : NPCState
{
    private Town_NPC npc;
    public Town_NPC_MoveState(NPC _npcBase, NPCStateMachine _stateMachine, string _animBoolName, Town_NPC npc) : base(_npcBase, _stateMachine, _animBoolName)
    {
        this.npc = npc;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        Vector3 dir = npc.target.position - npc.transform.position;
        npc.transform.Translate(dir.normalized * npc.moveSpeed * Time.deltaTime, Space.World);

        if (Vector3.Distance(npc.transform.position, npc.target.position) <= 0.48f)
        {
            GetNextWaypoint();
            npc.Flip();
            stateMachine.ChangeState(npc.idleState);
        }

    }

    private void GetNextWaypoint()
    {

        if (npc.wavePointIndex >= npc.waypoints.Length - 1)
        {
            npc.wavePointIndex = 0;
        }
        else
        {
            npc.wavePointIndex++;
        }

        npc.target = npc.waypoints[npc.wavePointIndex];
    }
}
