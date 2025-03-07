using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatWarrior_BattleState : EnemyState
{
    private Enemy_CatWarrior enemy;
    private Transform player;
    public CatWarrior_BattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_CatWarrior enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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

        enemy.anim.SetFloat("xVelocity", enemy.rb.velocity.x);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            if (enemy.IsPlayerDetected())
            {
                PlayerDetected();
            }
            else
            {
                stateMachine.ChangeState(enemy.idleState);
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

            if (enemy.IsPlayerDetected().distance <= enemy.rangeAttackDistance && CanMagicAttack())
                enemy.RangeAttack(player, enemy.magicState, enemy.evasionState, null);
            else if (enemy.IsPlayerDetected().distance <= enemy.meleeAttackDistance)
                enemy.MeleeAttack(player, enemy.attackState, enemy.evasionState, "CatWarriorAttack", false, 0);
            else
                enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);
        }
        else
        {

            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }

        enemy.BattleStateFlipControll(player);
    }

    public bool CanMagicAttack()
    {
        if (Time.time >= enemy.lastTimeRangeAttacked + enemy.rangeAttackCooldown)
        {
            return true;
        }
        return false;
    }
}
