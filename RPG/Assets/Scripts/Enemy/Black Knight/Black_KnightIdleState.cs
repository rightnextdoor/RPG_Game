using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightIdleState : EnemyState
{
    private Enemy_Black_Knight enemy;
    private Transform player;
    public Black_KnightIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = enemy.idleTime;
        player = PlayerManager.instance.player.transform;
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

        if (stateTimer < 0 && enemy.bossFightStart)
        {
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}
