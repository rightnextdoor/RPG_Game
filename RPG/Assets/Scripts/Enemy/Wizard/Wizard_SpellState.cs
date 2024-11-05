using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        if (enemy.IsPlayerDetected().distance < enemy.transform.position.x)
            enemy.Flip();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        enemy.SetZeroVelocity();
        if (enemy.IsPlayerDetected().distance < enemy.transform.position.x)
            enemy.Flip();

        if (!CanCastSpell())
            stateMachine.ChangeState(enemy.battleState);
    }

    public bool CanCastSpell()
    {
        //if (CanCastAirWalk())
        //{
        //    stateMachine.ChangeState(enemy.airWalkState);
        //    return true;
        //}

        //if (CanCastLightingStrike())
        //{
        //    stateMachine.ChangeState(enemy.lightingStrikeState);
        //    return true;
        //}

        //if (CanCastIceBall())
        //{
        //    stateMachine.ChangeState(enemy.iceBallState);
        //    return true;
        //}

        if (CanCastFireBall())
        {
            stateMachine.ChangeState(enemy.fireBallState);
            return true;
        }

        return false;
    }

    private bool CanCastFireBall()
    {
        if (Time.time >= enemy.lastTimeCastFireBall + enemy.fireBallCooldown)
        {
            enemy.isFireBall = true;
            return true;
        }
        return false;
    }

    private bool CanCastIceBall()
    {
        if (Time.time >= enemy.lastTimeCastIceBall + enemy.iceBallCooldown)
        {
            return true;
        }
        return false;
    }

    private bool CanCastLightingStrike()
    {
        if (Time.time >= enemy.lastTimeCastLightingStrike + enemy.lightingStrikeCooldown)
        {
            return true;
        }
        return false;
    }

    private bool CanCastAirWalk()
    {
        if (Time.time >= enemy.lastTimeCastAirWalk + enemy.airWalkCooldown)
        {
            return true;
        }
        return false;
    }
}
