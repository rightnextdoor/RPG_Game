using System;
using System.Collections.Generic;
using UnityEngine;

public class IdleStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private static readonly List<StateSound> Empty = new();

    private readonly Func<EnemyState> moveState;
    private readonly Func<EnemyState> battleState;

    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    public IdleStateBase(
        Enemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> moveState,
        Func<EnemyState> battleState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName)
    {
        this.moveState = moveState;
        this.battleState = battleState;
        this.enterSounds = enterSounds ?? Empty;
        this.exitSounds = exitSounds ?? Empty;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.idleTime;
        enemy.SetZeroVelocity();
        PlayAll(enterSounds);
    }

    public override void Update()
    {
        base.Update();
        stateTimer -= Time.deltaTime;

        EnemyState nextBattleState = battleState != null ? battleState() : null;
        if (nextBattleState != null && enemy.IsPlayerDetected())
        {
            stateMachine.ChangeState(nextBattleState);
            return;
        }

        if (stateTimer <= 0f)
        {
            if (enemy.IsWallDetected() || !enemy.IsGroundDetected() || enemy.boundaryTouchedOnMove)
                enemy.Flip();

            EnemyState nextMoveState = moveState != null ? moveState() : null;
            if (nextMoveState != null)
                stateMachine.ChangeState(nextMoveState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        PlayAll(exitSounds);
    }

    protected virtual void PlayAll(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var t = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(t);
    }
}