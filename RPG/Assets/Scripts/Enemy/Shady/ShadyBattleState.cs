using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadyBattleState : EnemyState
{
    private Enemy_Shady enemy;
    private Transform player;
    private int moveDir;

    private float defaultSpeed;

    public ShadyBattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Shady _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();

        player = PlayerManager.instance.player.transform;

        if (player.GetComponent<PlayerStats>().isDead)
            stateMachine.ChangeState(enemy.moveState);
    }

    public override void Exit()
    {
        base.Exit();

        //enemy.moveSpeed = defaultSpeed;
    }

    public override void Update()
    {
        base.Update();

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.moveState);
            return;
        }

        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;

            if (enemy.IsPlayerDetected().distance < enemy.attackDistance)
            {
                if (CanAttack())
                    stateMachine.ChangeState(enemy.attackState);
            }
        }
        else
        {
            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > 10)
                stateMachine.ChangeState(enemy.idleState);
        }

        if (player.position.x > enemy.transform.position.x)
            moveDir = 1;
        else if (player.position.x < enemy.transform.position.x)
            moveDir = -1;

        if (enemy.IsPlayerDetected() && enemy.IsPlayerDetected().distance < enemy.attackDistance - .5f)
            return;

        enemy.SetVelocity(enemy.moveSpeed * moveDir, rb.velocity.y);
    }

    private bool CanAttack()
    {
        if (Time.time >= enemy.lastTimeAttacked + enemy.attackCooldown)
        {
            enemy.attackCooldown = Random.Range(enemy.minAttackCooldown, enemy.maxAttackCooldown);
            enemy.lastTimeAttacked = Time.time;
            return true;
        }
        return false;
    }
}
