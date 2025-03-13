using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabMoveState : EnemyState
{
    private Enemy_Crab enemy;
    private Transform player;
    private float hitTimer = 0f;
    private float flip = 0f;
    private float flipTimer = 0f;
    public CrabMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Crab enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;
        stateTimer = enemy.moveTime;
        hitTimer = enemy.hitTimer;
        SetFlipTimer();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        hitTimer -= Time.deltaTime;
        flipTimer -= Time.deltaTime;

        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);
        
        if(hitTimer < 0f)
            enemy.RunIntoPlayerAttack(enemy.moveState);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
        }

        if (flipTimer < 0f)
        {
            int flip = Random.Range(1, 3);
            if (flip == 2)
                enemy.Flip();
            SetFlipTimer();
        }

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.idleState);
    }

    public void SetFlipTimer()
    {
        flip = Random.Range(enemy.moveTime / 2, enemy.moveTime - 1);
        flipTimer = flip;
    }

}
