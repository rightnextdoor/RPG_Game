using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightBattleState : EnemyState
{
    private Enemy_Knight enemy;
    private Player player;
    private int moveDir;
    private bool flippedOnce;
    private float defaultSpeed;
    public KnightBattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player;

        defaultSpeed = enemy.moveSpeed;

        enemy.moveSpeed = enemy.runSpeed;

        if (player.GetComponent<PlayerStats>().isDead)
            stateMachine.ChangeState(enemy.moveState);

        stateTimer = enemy.battleTime;
        flippedOnce = false;
    }

    public override void Exit()
    {
        base.Exit();
        enemy.moveSpeed = defaultSpeed;
    }

    public override void Update()
    {
        base.Update();

        enemy.anim.SetFloat("xVelocity", enemy.rb.velocity.x);

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
                {
                    stateMachine.ChangeState(enemy.attackState);
                    AudioManager.instance.PlaySFX("KnightAttack", enemy.transform);
                }
                // TODO: fix block to make player knockback
                //if (CanBlock())
                //{
                //    stateMachine.ChangeState(enemy.blockState);
                //}
            }
        }
        else
        {
            if (flippedOnce == false)
            {
                flippedOnce = true;
                enemy.Flip();
            }

            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > 7)
                stateMachine.ChangeState(enemy.idleState);
        }

        //float distanceToPlayerX = Mathf.Abs(player.position.x - enemy.transform.position.x);
        //if(distanceToPlayerX < 1f)
        //    return;  

        if (player.transform.position.x > enemy.transform.position.x)
            moveDir = 1;
        else if (player.transform.position.x < enemy.transform.position.x)
            moveDir = -1;

        if (enemy.IsPlayerDetected() && enemy.IsPlayerDetected().distance < enemy.attackDistance - .8f)
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

    private bool CanBlock()
    {
        if (player != null)
        {
            if (Time.time >= enemy.lastTimeBlock + enemy.blockCooldown)
            {
                enemy.blockCooldown = Random.Range(enemy.minBlockCooldown, enemy.maxBlockCooldown);
                enemy.lastTimeBlock = Time.time;
                return true;
            }
        }
        
        return false;
    }
}
