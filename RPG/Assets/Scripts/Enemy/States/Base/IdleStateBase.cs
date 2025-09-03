using System.Collections.Generic;
using UnityEngine;

public class IdleStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    protected virtual EnemyState MoveState => null;
    protected virtual EnemyState BattleState => null;

    private static readonly List<StateSound> Empty = new List<StateSound>(0);
    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    public IdleStateBase(
        Enemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName)
    {
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

        if (BattleState != null && enemy.IsPlayerDetected())
        {
            stateMachine.ChangeState(BattleState);
            return;
        }

        if (enemy.canPatrol && stateTimer <= 0f)
        {
            if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
                enemy.Flip();

            if (MoveState != null)
                stateMachine.ChangeState(MoveState);
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
