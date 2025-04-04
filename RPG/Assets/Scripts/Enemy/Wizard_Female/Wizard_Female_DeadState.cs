using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_Female_DeadState : EnemyState
{
    private Enemy_Wizard_Female enemy;
    public Wizard_Female_DeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard_Female enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }
    public override void Enter()
    {
        base.Enter();

        //AudioManager.instance.PlaySFX("ArcherDie", enemy.transform);
        enemy.stats.MakeInvincible(true);
    }

    public override void Update()
    {
        base.Update();

        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }
}
