using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiranhaPlantAttackState : EnemyState
{
    private Enemy_PiranhaPlant enemy;
    public PiranhaPlantAttackState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_PiranhaPlant enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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

        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }
}
