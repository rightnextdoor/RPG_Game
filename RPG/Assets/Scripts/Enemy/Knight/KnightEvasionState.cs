using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightEvasionState : EnemyState
{
    private Enemy_Knight enemy;
    private Transform player;
    private int moveDir;

    public KnightEvasionState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerUtils.GetPlayerSafe().transform;
        stateTimer = enemy.evasionTimer;

        if (player.position.x > enemy.transform.position.x)
        {
            moveDir = 1;
        }
        else if (player.position.x < enemy.transform.position.x)
        {
            moveDir = -1;
        }

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
            moveDir = -moveDir;
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
            enemy.SetZeroVelocity();
            enemy.Flip();
            stateMachine.ChangeState(enemy.battleState);
            return;
        }

        enemy.SetVelocity(enemy.evasionSpeed * moveDir, rb.linearVelocity.y);

        if (stateTimer < 0)
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}
