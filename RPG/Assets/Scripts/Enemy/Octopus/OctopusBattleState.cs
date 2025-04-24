using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctopusBattleState : EnemyState
{
    private Enemy_Octopus enemy;
    private Transform player;

    public OctopusBattleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Octopus enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
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

            if (enemy.IsPlayerDetected().distance < enemy.rangeAttackDistance)
            {
                enemy.anim.SetFloat("Battle", 0);
            }
            enemy.RangeAttack(player, enemy.attackState, enemy.evasionState, "OctopusAttack");
        }
        else
        {
            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.moveState);
        }

        enemy.BattleStateFlipControll(player);
    }

}
