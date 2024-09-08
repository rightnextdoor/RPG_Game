using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonIdleState : SkeletonGroundedState
{
    public SkeletonIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy) : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
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

        AudioManager.instance.PlaySFX("SkeletonIdle", enemy.transform);
    }

    public override void Update()
    {
        base.Update();

        if (enemy.IsPlayerDetected() && !enemy.IsGroundDetected() || enemy.IsWallDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        } 
        else
        {
            stateMachine.ChangeState(enemy.moveState);
        }

        if (stateTimer < 0f)
            stateMachine.ChangeState(enemy.moveState);

    }
}
