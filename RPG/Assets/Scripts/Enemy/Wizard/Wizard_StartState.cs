using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_StartState : EnemyState
{
    private Enemy_Wizard enemy;
    public Wizard_StartState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }
    public override void Enter()
    {
        base.Enter();

        stateTimer = enemy.idleTime;

    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0 && enemy.bossFightStart)
        {
            stateMachine.ChangeState(enemy.idleState);
            enemy.StartNextStage();
        }
    }
}
