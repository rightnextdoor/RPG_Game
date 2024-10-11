using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctopusMoveState : EnemyState
{
    private Enemy_Octopus enemy;
    private Transform player;

    private float waitTimer;
    public OctopusMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Octopus enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;
        stateTimer = enemy.moveTime;
        waitTimer = 1.5f;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        waitTimer -= Time.deltaTime;

        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
        }

        if (stateTimer < 0f)
        {
            int flip = Random.Range(1, 3);
            if (flip == 2)
                enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
        }

        if (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position, player.transform.position) < enemy.agroDistance)
            stateMachine.ChangeState(enemy.battleState);

        if (Vector2.Distance(enemy.transform.position, player.transform.position) > enemy.agroDistance + 2 && waitTimer < 0)
            stateMachine.ChangeState(enemy.waterState);
    }
}
