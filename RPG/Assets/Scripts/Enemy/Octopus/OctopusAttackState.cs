using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctopusAttackState : EnemyState
{
    private Enemy_Octopus enemy;

    public OctopusAttackState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Octopus enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        enemy.anim.SetFloat("Battle", 1);
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
