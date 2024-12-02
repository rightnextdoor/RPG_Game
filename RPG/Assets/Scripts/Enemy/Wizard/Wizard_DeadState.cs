using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_DeadState : EnemyState
{
    private Enemy_Wizard enemy;
    public Wizard_DeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("Wizard_Die", null);
        AudioManager.instance.PlaySFXWithDelay("Wizard_TeleportOut", null, .6f);
        AudioManager.instance.PlaySFXWithDelay("Phoenix", null, 1.7f);
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
