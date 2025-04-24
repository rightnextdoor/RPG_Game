using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadyBattleState : EnemyState
{
    private Enemy_Shady enemy;
    private Transform player;

    public ShadyBattleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Shady _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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

            if (enemy.IsPlayerDetected().distance <= enemy.rangeAttackDistance &&
                enemy.IsPlayerDetected().distance > enemy.rangeAttackDistance / 2)
                enemy.RangeAttack(player, enemy.attackState, enemy.evasionState, null);
            else 
            if(enemy.IsPlayerDetected().distance <= enemy.meleeAttackDistance)
                enemy.MeleeAttack(player, enemy.meleeAttack, enemy.evasionState, "ShadyAttack", false, 0);
            else
            {
                if (!enemy.IsGroundDetected())
                {
                    stateMachine.ChangeState(enemy.idleState);
                    return;
                }
                enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);
            }



        }
        else
        {

            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }

        enemy.BattleStateFlipControll(player);

    }
}
