using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightBorne_DeadState : EnemyState
{
    private Enemy_NightBorne enemy;
    public NightBorne_DeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_NightBorne enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }
    public override void Enter()
    {
        base.Enter();

        //AudioManager.instance.PlaySFX("SkeletonDie", enemy.transform);
        enemy.stats.MakeInvincible(true);

    }

    public override void Update()
    {
        base.Update();
        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }
}
