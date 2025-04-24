using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonWarrior_BattleState : EnemyState
{
    private Enemy_DragonWarrior enemy;
    private Transform player;

    public DragonWarrior_BattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_DragonWarrior enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerUtils.GetPlayerSafe().transform;

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

            enemy.MeleeAttack(player, enemy.attackState, null, null, false, 0);
        }
        else
        {

            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }

        enemy.BattleStateFlipControll(player);

    }
}
