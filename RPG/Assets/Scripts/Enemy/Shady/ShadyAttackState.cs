using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadyAttackState : EnemyState
{
    private Enemy_Shady enemy;
    public ShadyAttackState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Shady enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();

        enemy.lastTimeAttacked = Time.time;
    }

    public override void Update()
    {
        base.Update();

        enemy.SetZeroVelocity();

        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }
}
