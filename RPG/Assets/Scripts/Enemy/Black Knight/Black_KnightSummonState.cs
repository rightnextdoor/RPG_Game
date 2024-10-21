using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightSummonState : EnemyState
{
    private Enemy_Black_Knight enemy;

    public Black_KnightSummonState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.stats.MakeInvincible(true);
        enemy.SummonSkeleton();
    }

    public override void Exit()
    {
        base.Exit();

        enemy.stats.MakeInvincible(false);
        enemy.SetZeroVelocity();
        enemy.restartCooldown = true;
    }

    public override void Update()
    {
        base.Update();
        
        if (enemy.IsSummonOver())
        {
            stateMachine.ChangeState(enemy.idleState);
        }

    }

}
