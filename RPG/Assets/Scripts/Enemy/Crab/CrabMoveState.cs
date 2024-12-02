using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabMoveState : EnemyState
{
    private Enemy_Crab enemy;
    public CrabMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Crab enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
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

        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);

        Attack();

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.idleState);
        }

        if (stateTimer < 0f)
        {
            int flip = Random.Range(1, 3);
            if (flip == 2)
                enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
        }
    }

    private void Attack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheck.position, enemy.attackCheckRadius);

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                PlayerStats target = hit.GetComponent<PlayerStats>();
                if (enemy.hitCountdown < 0f)
                {
                    enemy.stats.DoDamage(target);
                    //ToDo: knock the player back when hit
                    enemy.hitCountdown = enemy.hitTimer;
                }

            }
        }
    }
}
