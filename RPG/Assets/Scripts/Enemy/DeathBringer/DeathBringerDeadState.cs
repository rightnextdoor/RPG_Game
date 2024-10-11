using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBringerDeadState : EnemyState
{
    private Enemy_DeathBringer enemy;
    public DeathBringerDeadState(Enemy_Boss _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_DeathBringer _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("DeathBringerDie", null);
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
