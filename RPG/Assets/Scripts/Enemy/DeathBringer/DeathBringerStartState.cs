using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBringerStartState : EnemyState
{
    private Enemy_DeathBringer enemy;
    public DeathBringerStartState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_DeathBringer enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
            stateMachine.ChangeState(enemy.battleState);
            enemy.StartNextStage();
        }
    }
}
