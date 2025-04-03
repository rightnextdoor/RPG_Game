using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_Male_DeadState : EnemyState
{
    private Enemy_Wizard_Male enemy;
    public Wizard_Male_DeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard_Male enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
