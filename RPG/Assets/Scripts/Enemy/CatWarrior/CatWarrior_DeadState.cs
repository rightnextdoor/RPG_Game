using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatWarrior_DeadState : EnemyState
{
    private Enemy_CatWarrior enemy;
    public CatWarrior_DeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_CatWarrior enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("CatWarriorDie", enemy.transform);
        enemy.stats.MakeInvincible(true);

    }

    public override void Update()
    {
        base.Update();
        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }
}
