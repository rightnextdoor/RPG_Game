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

        //AudioManager.instance.PlaySFX("ArcherJump", enemy.transform);
        rb.velocity = new Vector2(enemy.jumpVelocity.x * enemy.facingDir, enemy.jumpVelocity.y);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.Attack();

        if (rb.velocity.y < 0 && enemy.IsGroundDetected())
        {
            if (!enemy.IsPlayerDetected())
            {
                int flip = Random.Range(1, 3);
                if (flip == 2)
                    enemy.Flip();
            }
            stateMachine.ChangeState(enemy.idleState);
        }
    }
    
}
