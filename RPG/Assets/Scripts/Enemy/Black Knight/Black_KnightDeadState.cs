using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightDeadState : EnemyState
{
    private Enemy_Black_Knight enemy;
    public Black_KnightDeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        //AudioManager.instance.PlaySFX("DeathBringerDie", null);
        enemy.stats.MakeInvincible(true);
        enemy.SelfDestroy();
    }

    public override void Update()
    {
        base.Update();

        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }
}
