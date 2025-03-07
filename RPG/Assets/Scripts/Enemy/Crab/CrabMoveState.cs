using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabMoveState : EnemyState
{
    private Enemy_Crab enemy;
    private float hitTimer = 0f;
    public CrabMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Crab enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.moveTime;
        hitTimer = enemy.hitTimer;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);
        hitTimer -= Time.deltaTime;
        if(hitTimer < 0f)
            enemy.RunIntoPlayerAttack(enemy.moveState);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.Flip();
        }

        if (stateTimer < 0f)
        {
            int flip = Random.Range(1, 3);
            if (flip == 2)
                enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
        }
    }

}
