using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Wizard_SpellState : EnemyState
{
    private Enemy_Wizard enemy;
    private float waitTimer;
    public Wizard_SpellState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        waitTimer = .15f;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        waitTimer -= Time.deltaTime;

        enemy.SetZeroVelocity();

        if (waitTimer < 0 && enemy.IsPlayerDetected())
        {
            if (!CanCastSpell())
                stateMachine.ChangeState(enemy.battleState);
        }
    }

    public bool CanCastSpell()
    {
        //if (CanCastAirWalk())
        //{
        //    stateMachine.ChangeState(enemy.airWalkState);
        //    return true;
        //}

        if (CanCastLightingStrike())
        {
            stateMachine.ChangeState(enemy.lightingStrikeState);
            return true;
        }

        if (CanCastIceBall())
        {
            stateMachine.ChangeState(enemy.iceBallState);
            return true;
        }

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
            enemy.isIceBall = true;
            return true;
        }
        return false;
    }

    private bool CanCastLightingStrike()
    {
        if (Time.time >= enemy.lastTimeCastLightingStrike + enemy.lightingStrikeCooldown)
        {
            enemy.isLightingStrike = true;
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
