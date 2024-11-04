using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_SpellState : EnemyState
{
    private Enemy_Wizard enemy;
    public Wizard_SpellState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
    }

    public override void Update()
    {
        base.Update();

        if (enemy.CanCastSpell())
        {
            stateMachine.ChangeState(enemy.battleState);
        }
        else
        {
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}
