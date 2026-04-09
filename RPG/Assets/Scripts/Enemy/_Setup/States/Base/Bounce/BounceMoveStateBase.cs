using System;
using System.Collections.Generic;

public class BounceMoveStateBase<TEnemy> : BounceStateBase<TEnemy> where TEnemy : Enemy_Regular
{
    protected readonly Func<EnemyState> idleState;
    protected readonly Func<EnemyState> battleState;

    public BounceMoveStateBase(
        TEnemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> idleState,
        Func<EnemyState> battleState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName, enterSounds, exitSounds)
    {
        this.idleState = idleState;
        this.battleState = battleState;
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