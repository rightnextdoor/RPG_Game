using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightDeadState : EnemyState
{
    private Enemy_Knight enemy;
    public KnightDeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("KnightDie" + enemy.knightType, enemy.transform);
        enemy.stats.MakeInvincible(true);

    }

    public override void Update()
    {
        base.Update();
        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }
}
