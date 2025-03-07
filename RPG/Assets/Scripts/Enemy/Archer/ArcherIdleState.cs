using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherIdleState : ArcherGroundedState
{
    public ArcherIdleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Archer _enemy) : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
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
