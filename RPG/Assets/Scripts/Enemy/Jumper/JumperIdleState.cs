using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumperIdleState : EnemyState
{
    private Enemy_Jumper enemy;
    private Transform player;
    public JumperIdleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Jumper enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = enemy.idleTime;
        player = PlayerUtils.GetPlayerSafe().transform;
        enemy.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void Update()
    {
        base.Update();

        enemy.Attack();

        if (enemy.IsWallDetected())
        {
            if (stateTimer < 0f)
            {
                enemy.Flip();
                stateMachine.ChangeState(enemy.jumpState);
            }
        }
        if (stateTimer < 0f)
        {
            if (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position, player.transform.position) < enemy.agroDistance)
                stateMachine.ChangeState(enemy.battleState);

            stateMachine.ChangeState(enemy.jumpState);
        }       
    }

}
