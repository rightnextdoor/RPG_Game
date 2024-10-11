using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherDeadState : EnemyState
{
    private Enemy_Archer enemy;
    public ArcherDeadState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Archer _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("ArcherDie", enemy.transform);
        enemy.stats.MakeInvincible(true);
    }

    public override void Update()
    {
        base.Update();

        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }
}
