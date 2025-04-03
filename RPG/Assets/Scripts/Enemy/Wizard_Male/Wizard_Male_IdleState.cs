using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_Male_IdleState : Wizard_Male_GroundedState
{
    public Wizard_Male_IdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard_Male enemy) : base(_enemyBase, _stateMachine, _animBoolName, enemy)
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
