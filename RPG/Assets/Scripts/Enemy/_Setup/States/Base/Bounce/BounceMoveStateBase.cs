using System;
using System.Collections.Generic;
using UnityEngine;

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

        if (!needsPatrolReturn)
            needsPatrolReturn = enemy.IsOutsidePatrol(enemy.transform.position.x);

        if (currentPhase != BouncePhase.Complete)
            return;

        if (!launchReady)
        {
            if (idleState != null)
                stateMachine.ChangeState(idleState());

            return;
        }

        if (needsPatrolReturn && CanJumpToPatrolCenter())
            needsPatrolReturn = false;

        PlanJump();
        currentPhase = BouncePhase.Launch;
    }

    public override void Update()
    {
        base.Update();  
    }

    public override void Exit()
    {
        base.Exit();
    }

    #region Helper
    protected virtual float GetPatrolSpaceForSide(BounceJumpSide jumpSide)
    {
        float currentX = enemy.transform.position.x;

        if (jumpSide == BounceJumpSide.Left)
            return Mathf.Max(0f, currentX - enemy.patrolLeftX);

        return Mathf.Max(0f, enemy.patrolRightX - currentX);
    }
    #endregion

    #region Return to patrol

    protected virtual bool CanJumpToPatrolCenter()
    {
        BounceJumpSide sideToCenter = GetReturnSideToPatrolCenter();
        float distanceToCenter = GetDistanceToPatrolCenter();
        float maxDistance = GetJumpMaxDistance(sideToCenter);

        return distanceToCenter <= maxDistance;
    }

    protected virtual float GetDistanceToPatrolCenter()
    {
        return Mathf.Abs(enemy.patrolCenter.x - enemy.transform.position.x);
    }

    protected virtual BounceJumpSide GetReturnSideToPatrolCenter()
    {
        return enemy.transform.position.x < enemy.patrolCenter.x
            ? BounceJumpSide.Right
            : BounceJumpSide.Left;
    }
    #endregion

    #region Override

    protected override void OnAttachComplete()
    {
        if (idleState != null)
            stateMachine.ChangeState(idleState());
    }
    protected override BounceJumpSide GetJumpSide()
    {
        if (needsPatrolReturn)
            return GetReturnSideToPatrolCenter();

        return base.GetJumpSide();
    }

    protected override float GetJumpDistance(BounceJumpSide jumpSide)
    {
        if (needsPatrolReturn && CanJumpToPatrolCenter())
            return GetDistanceToPatrolCenter();

        return base.GetJumpDistance(jumpSide);
    }

    protected override float GetJumpMaxDistance(BounceJumpSide jumpSide)
    {
        float baseMaxDistance = base.GetJumpMaxDistance(jumpSide);

        if (needsPatrolReturn)
            return baseMaxDistance;

        float patrolSpace = GetPatrolSpaceForSide(jumpSide);

        if (patrolSpace < baseMaxDistance)
            return patrolSpace;

        return baseMaxDistance;
    }

    #endregion
}