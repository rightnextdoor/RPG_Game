using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightEvasionState : EnemyState
{
    private Enemy_Black_Knight enemy;
    private Transform player;
    private int moveDir;
    private float evadeTimer;
    public Black_KnightEvasionState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;
        evadeTimer = 1.5f;

        if (player.position.x > enemy.transform.position.x)
        {
            moveDir = -1;
        }
        else if (player.position.x < enemy.transform.position.x)
        {
            moveDir = 1;
        }

        if (enemy.IsBackWallDetected() || !enemy.IsBackGroundDetected())
            moveDir = -moveDir;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        evadeTimer -= Time.deltaTime;

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.SetZeroVelocity();
            enemy.Flip();
            stateMachine.ChangeState(enemy.laughState);
            return;
        }

        enemy.SetVelocity(enemy.evasionSpeed * moveDir, rb.velocity.y);

        if (evadeTimer < 0)
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.laughState);
        }
    }
}
