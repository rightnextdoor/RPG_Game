using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadyIdleState : ShadyGroundedState
{
    public ShadyIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Shady _enemy) : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = enemy.idleTime;
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void Update()
    {
        base.Update();

        if (enemy.IsPlayerDetected() && !enemy.IsGroundDetected() || enemy.IsWallDetected())
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        if (stateTimer < 0f)
            stateMachine.ChangeState(enemy.moveState);

    }
}
