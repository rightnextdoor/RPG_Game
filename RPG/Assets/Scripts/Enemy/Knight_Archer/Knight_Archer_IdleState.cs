using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight_Archer_IdleState : Knight_Archer_GroundedState
{
    public Knight_Archer_IdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight_Archer enemy) : base(_enemyBase, _stateMachine, _animBoolName, enemy)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = enemy.idleTime;
        enemy.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void Update()
    {
        base.Update();

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            if (stateTimer < 0f)
            {
                if (enemy.canPatrol)
                {
                    enemy.Flip();
                    stateMachine.ChangeState(enemy.moveState);
                }
            }
        }

        if (stateTimer < 0f && enemy.canPatrol)
            stateMachine.ChangeState(enemy.moveState);
    }
}
