using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabGroundedState : EnemyState
{
    protected Enemy_Crab enemy;
    protected Transform player;
    public CrabGroundedState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Crab enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position, player.transform.position) < enemy.agroDistance)
        {
            if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
                enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
        }
    }
}
