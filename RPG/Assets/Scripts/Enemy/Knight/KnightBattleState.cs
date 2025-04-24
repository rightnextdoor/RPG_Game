using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightBattleState : EnemyState
{
    private Enemy_Knight enemy;
    private Transform player;
    private float defaultSpeed;
    public KnightBattleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerUtils.GetPlayerSafe().transform;

        defaultSpeed = enemy.moveSpeed;

        enemy.moveSpeed = enemy.runSpeed;

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
        enemy.moveSpeed = defaultSpeed;
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

            enemy.MeleeAttack(player, enemy.attackState, enemy.evasionState, "KnightAttack", false, 0);
        }
        else
        {

            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }

        enemy.BattleStateFlipControll(player);

    }
}
