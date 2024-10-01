using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightBlockState : EnemyState
{
    private Enemy_Knight enemy;
    private Player player;
    public KnightBlockState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.blockDuration;
        player = PlayerManager.instance.player;
        enemy.stats.MakeInvincible(true);
    }

    public override void Exit()
    {
        base.Exit();
        enemy.stats.MakeInvincible(false);
    }

    public override void Update()
    {
        base.Update();

        enemy.SetZeroVelocity();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(player.attackCheck.position, player.attackCheckRadius);

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                if (hit.GetComponent<Player>().CanBeBlocked())
                {
                    SuccesfulBlockAttack();
                }
            }
        }

        if (stateTimer < 0 || triggerCalled)
            stateMachine.ChangeState(enemy.idleState);
    }

    private void SuccesfulBlockAttack()
    {
        stateTimer = 1;
        Debug.Log("block player's attack");
    }
}
