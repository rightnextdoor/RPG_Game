using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumperBattleState : EnemyState
{
    private Enemy_Jumper enemy;
    private Transform player;
    private int moveDir;
    public JumperBattleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Jumper enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;

        if (player.GetComponent<PlayerStats>().isDead)
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        stateTimer = enemy.battleTime;
        enemy.BattleStateFlipControll(player);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.Attack();

        PlayerDetected();
        
    }

    private void PlayerDetected()
    {
        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;
            if (CanJump())
            {
                stateMachine.ChangeState(enemy.jumpState);
                AudioManager.instance.PlaySFX("JumperJump", enemy.transform);
            }
        }
        else
        {
            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }
    }

    private bool CanJump()
    {
        if (enemy.GroundBehind() == false || enemy.WallBehind() == true)
            return false;

        if (Time.time >= enemy.lastTimeJumped + enemy.rangeAttackCooldown)
        {
            enemy.rangeAttackCooldown = Random.Range(enemy.minRangeAttackCooldown, enemy.maxRangeAttackCooldown);
            enemy.lastTimeJumped = Time.time;
            return true;
        }

        return false;
    }
}
