using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_Male_MoveState : Wizard_Male_GroundedState
{
    public Wizard_Male_MoveState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard_Male enemy) : base(_enemyBase, _stateMachine, _animBoolName, enemy)
    {
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.moveTime;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.linearVelocity.y);


        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {

            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
