using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightStartState : EnemyState
{
    private Enemy_Black_Knight enemy;
    public Black_KnightStartState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
