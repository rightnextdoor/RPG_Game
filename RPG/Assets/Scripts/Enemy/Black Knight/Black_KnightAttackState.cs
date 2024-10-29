using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightAttackState : EnemyState
{
    private Enemy_Black_Knight enemy;
    public Black_KnightAttackState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
        {
            if (CanEvade())
            {
                stateMachine.ChangeState(enemy.evasionState);
            }
            else
                stateMachine.ChangeState(enemy.idleState);
        }
    }

    private bool CanEvade()
    {
        if (Time.time >= enemy.lastTimeEvade + enemy.evasionCooldown)
        {
            enemy.evasionCooldown = Random.Range(enemy.minEvasionCooldown, enemy.maxEvasionCooldown);
            enemy.lastTimeEvade = Time.time;
            return true;
        }
        return false;
    }
}
