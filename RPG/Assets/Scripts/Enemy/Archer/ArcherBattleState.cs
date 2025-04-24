using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherBattleState : EnemyState
{
    private Enemy_Archer enemy;
    private Transform player;

    public ArcherBattleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Archer _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerUtils.GetPlayerSafe().transform;

        if (player.GetComponent<PlayerStats>().isDead)
        {
            stateMachine.ChangeState(enemy.moveState);
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

        enemy.anim.SetFloat("xVelocity", enemy.rb.velocity.x);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            if (enemy.IsPlayerDetected())
            {
                PlayerDetected();
            }
            else
            {
                stateMachine.ChangeState(enemy.moveState);
            }
            return;
        }

        PlayerDetected();

    }

    private void PlayerDetected()
    {
        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;
            enemy.RangeAttack(player, enemy.attackState, enemy.evasionState, null);

            if (enemy.IsPlayerDetected().distance < enemy.safeDistance)
            {
                if (CanJump() && enemy.canJump)
                    stateMachine.ChangeState(enemy.jumpState);
            }

        }
        else
        {
            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }

        enemy.BattleStateFlipControll(player);
    }

    private bool CanJump()
    {
        if(enemy.GroundBehind() == false || enemy.WallBehind() == true)
            return false;

        if (Time.time >= enemy.lastTimeJumped + enemy.jumpCooldown)
        {
            enemy.lastTimeJumped = Time.time;
            return true;
        }

        return false;
    }
}
