using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightSummonState : EnemyState
{
    private Enemy_Black_Knight enemy;
    private float summonTimer;
    private int amountOfSummons;
    public Black_KnightSummonState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        summonTimer = .15f;
        //amountOfSummons = enemy.amountOfSummons;
        enemy.stats.MakeInvincible(true);
        enemy.SummonSkeleton();
    }

    public override void Exit()
    {
        base.Exit();

        enemy.stats.MakeInvincible(false);
        enemy.lastTimeSummon = Time.time;
        enemy.SetZeroVelocity();
    }

    public override void Update()
    {
        base.Update();

        summonTimer -= Time.deltaTime;
        
        if (enemy.IsSummonOver())
        {
            stateMachine.ChangeState(enemy.idleState);
        }

    }
    //private bool CanSummon()
    //{
    //    if (summonTimer < 0)
    //    {
    //        //amountOfSummons--;
    //        summonTimer = enemy.summonTimer;
    //        return true;
    //    }
    //    return false;
    //}
}
