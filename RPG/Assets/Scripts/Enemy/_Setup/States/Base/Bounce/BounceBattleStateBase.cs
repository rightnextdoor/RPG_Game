using System;
using System.Collections.Generic;

public class BounceBattleStateBase<TEnemy> : BounceStateBase<TEnemy> where TEnemy : Enemy_Regular
{
    protected readonly Func<EnemyState> idleState;
    protected readonly Func<EnemyState> moveState;
    protected readonly Func<EnemyState> runIntoState;

    public BounceBattleStateBase(
        TEnemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> idleState,
        Func<EnemyState> moveState,
        Func<EnemyState> runIntoState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName, enterSounds, exitSounds)
    {
        this.idleState = idleState;
        this.moveState = moveState;
        this.runIntoState = runIntoState;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Exit()
    {
        base.Exit();
    }
}