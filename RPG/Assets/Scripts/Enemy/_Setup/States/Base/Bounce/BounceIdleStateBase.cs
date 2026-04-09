using System;
using System.Collections.Generic;
using UnityEngine;

public class BounceIdleStateBase<TEnemy> : BounceStateBase<TEnemy> where TEnemy : Enemy_Regular
{
    protected readonly Func<EnemyState> moveState;
    protected readonly Func<EnemyState> battleState;

    public BounceIdleStateBase(
        TEnemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> moveState,
        Func<EnemyState> battleState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName, enterSounds, exitSounds)
    {
        this.moveState = moveState;
        this.battleState = battleState;
    }

    public override void Enter()
    {
        base.Enter();

        if (!timerSet)
        {
            bounceTimer = GetBounceWaitTime();
            timerSet = true;
            launchReady = false;
        }
    }

    public override void Update()
    {
        base.Update();

        if (!launchReady)
        {
            bounceTimer -= Time.deltaTime;

            if (bounceTimer <= 0f)
            {
                bounceTimer = 0f;
                launchReady = true;
            }
        }

        if (IsPlayerInSelectedEntryRange())
        {
            if (battleState != null)
                stateMachine.ChangeState(battleState());

            return;
        }

        if (IsPlayerTooClose())
        {
            if (battleState != null)
                stateMachine.ChangeState(battleState());

            return;
        }

        if (launchReady)
        {
            if (moveState != null)
                stateMachine.ChangeState(moveState());

            return;
        }
    }

    public override void Exit()
    {
        if (!launchReady)
        {
            base.Exit();
            return;
        }

        PlayAll(exitSounds);
        TypedExit();
    }

    protected virtual float GetBounceWaitTime()
    {
        float min = enemy.bounceWaitMin;
        float max = enemy.bounceWaitMax;

        const float fallbackTime = 0.05f;

        if (min <= 0f)
            min = fallbackTime;

        if (max <= 0f)
            max = fallbackTime;

        if (max < min)
            max = min;

        return UnityEngine.Random.Range(min, max);
    }

    protected virtual void TypedExit()
    {
        base.Exit();
    }
}