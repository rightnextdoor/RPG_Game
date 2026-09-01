using System.Collections.Generic;
using UnityEngine;

public partial class BounceStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private static readonly List<StateSound> Empty = new();

    protected readonly List<StateSound> enterSounds;
    protected readonly List<StateSound> exitSounds;

    protected readonly BounceMovementHandler<TEnemy> movementHandler;

    #region Bounce runtime

    #region Enemy's get/set fields
    protected BouncePhase currentPhase
    {
        get => (BouncePhase)enemy.bouncePhase;
        set => enemy.bouncePhase = (int)value;
    }
    protected bool timerSet
    {
        get => enemy.bounceTimerSet;
        set => enemy.bounceTimerSet = value;
    }
    protected bool launchReady
    {
        get => enemy.bounceLaunchReady;
        set => enemy.bounceLaunchReady = value;
    }

    #endregion

    #region State Settings
    protected float bounceTimer;
    protected bool needsPatrolReturn;
    #endregion

    #region Surface Settings
    protected BounceSurface currentSurface = BounceSurface.Floor;
    protected Vector2 currentSurfaceNormal = Vector2.up;


    #endregion

    #region Phase Settings
    protected bool isAttached;
    protected bool isAirborne;
    protected float savedDefaultGravity;
    protected bool gravityCached;

    protected Vector2 landingPoint;
    protected bool hasLandingHit;

    #endregion

    #region Plan Settings
    protected BounceJumpType plannedJumpType = BounceJumpType.Normal;
    protected BounceJumpSide plannedJumpSide = BounceJumpSide.Right;
    protected BounceAirMoveType plannedAirMoveType = BounceAirMoveType.None;

    protected Vector2 plannedJumpDirection = Vector2.right;
    protected float plannedJumpDistance;
    protected float plannedJumpHeight;
    protected BounceSurface launchSurface = BounceSurface.Floor;

    protected Vector2 plannedArcAwayAxis = Vector2.up;
    protected float plannedArcStartZ;

    #endregion

    #endregion

    #region State
    public BounceStateBase(
        TEnemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName)
    {
        this.enterSounds = enterSounds ?? Empty;
        this.exitSounds = exitSounds ?? Empty;

        movementHandler = new BounceMovementHandler<TEnemy>(this, enemyBase);
    }

    public override void Enter()
    {
        base.Enter();

        if (!System.Enum.IsDefined(typeof(BouncePhase), enemy.bouncePhase))
            currentPhase = BouncePhase.Attach;

        CacheDefaultGravity();
        PlayAll(enterSounds);
    }

    public override void Update()
    {
        base.Update();
        UpdateBouncePhase();
    }

    public override void Exit()
    {
        base.Exit();
        PlayAll(exitSounds);
    }

    protected virtual void UpdateBouncePhase()
    {
        switch (currentPhase)
        {
            case BouncePhase.Attach:
                AttachPhase();
                break;

            case BouncePhase.Launch:
                LaunchPhase();
                break;

            case BouncePhase.Airborne:
                AirbornePhase();
                break;

            case BouncePhase.Complete:
                CompletePhase();
                break;
        }
    }

    #endregion

    #region Movement handler bridge

    internal Rigidbody2D MovementRigidbody => rb;

    internal BounceSurface MovementCurrentSurface
    {
        get => currentSurface;
        set => currentSurface = value;
    }

    internal Vector2 MovementCurrentSurfaceNormal
    {
        get => currentSurfaceNormal;
        set => currentSurfaceNormal = value;
    }

    internal BounceAirMoveType MovementPlannedAirMoveType
    {
        get => plannedAirMoveType;
        set => plannedAirMoveType = value;
    }

    internal BounceJumpSide MovementPlannedJumpSide
    {
        get => plannedJumpSide;
        set => plannedJumpSide = value;
    }

    internal Vector2 MovementPlannedJumpDirection
    {
        get => plannedJumpDirection;
        set => plannedJumpDirection = value;
    }

    internal float MovementPlannedJumpDistance
    {
        get => plannedJumpDistance;
        set => plannedJumpDistance = value;
    }

    internal float MovementPlannedJumpHeight => plannedJumpHeight;
    internal BounceSurface MovementLaunchSurface => launchSurface;

    internal Vector2 MovementPlannedArcAwayAxis
    {
        get => plannedArcAwayAxis;
        set => plannedArcAwayAxis = value;
    }

    internal float MovementPlannedArcStartZ
    {
        get => plannedArcStartZ;
        set => plannedArcStartZ = value;
    }

    internal Vector2 MovementGetLocalJumpDirection(BounceJumpSide jumpSide)
    {
        return GetLocalJumpDirection(jumpSide);
    }

    internal Vector2 MovementGetJumpSideProbeOrigin(Collider2D col, Vector2 direction)
    {
        return GetJumpSideProbeOrigin(col, direction);
    }

    internal float MovementGetJumpSideProbeDistance(Collider2D col)
    {
        return GetJumpSideProbeDistance(col);
    }

    internal float MovementGetFloorToCeilingSpan()
    {
        return GetFloorToCeilingSpan();
    }

    internal float MovementGetSurfaceClearance()
    {
        return GetSurfaceClearance();
    }

    internal void MovementSetAttachedGravity()
    {
        SetAttachedGravity();
    }

    internal void MovementSetAirborneGravity()
    {
        SetAirborneGravity();
    }

    #endregion
    #region Distance checks

    protected virtual bool IsPlayerInSelectedEntryRange()
    {
        return false;
    }

    protected virtual bool IsPlayerTooClose()
    {
        return false;
    }

    #endregion


    protected virtual void PlayAll(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;

        var t = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(t);
    }

}
