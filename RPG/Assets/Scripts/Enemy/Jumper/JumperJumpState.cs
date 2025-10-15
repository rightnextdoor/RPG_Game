using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumperJumpState : EnemyState
{
    private Enemy_Jumper enemy;
    public JumperJumpState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Jumper enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.SetVelocity(enemy.jumpVelocity.x * enemy.facingDir, enemy.jumpVelocity.y);
        stateTimer = enemy.fallTimer;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.Attack();
        

        if (rb.linearVelocity.y < 0 && enemy.IsGroundDetected())
        {
            if (enemy.IsWallDetected())
            {
                stateMachine.ChangeState(enemy.idleState);
            }

            if (!enemy.IsPlayerDetected())
            {
                int flip = Random.Range(1, 5);
                if (flip == 2)
                    enemy.Flip();
                stateMachine.ChangeState(enemy.idleState);
            }

            if (stateTimer < 0f)
            {
                if (enemy.IsWallDetected())
                {
                    stateMachine.ChangeState(enemy.idleState);
                }
                stateMachine.ChangeState(enemy.idleState);
            }
        }
    }
    
}
