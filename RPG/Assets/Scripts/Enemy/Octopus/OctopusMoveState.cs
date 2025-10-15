using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctopusMoveState : EnemyState
{
    private Enemy_Octopus enemy;
    private Transform player;

    public OctopusMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Octopus enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerUtils.GetPlayerSafe().transform;
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
            enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
        }

        if (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position, player.transform.position) < enemy.agroDistance)
            stateMachine.ChangeState(enemy.battleState);

        if (Vector2.Distance(enemy.transform.position, player.transform.position) > enemy.playerDistance && stateTimer < 0)
            stateMachine.ChangeState(enemy.waterState);
    }
}
