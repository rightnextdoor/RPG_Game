using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight_Archer_DeadState : EnemyState
{
    private Enemy_Knight_Archer enemy;
    public Knight_Archer_DeadState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Knight_Archer enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
