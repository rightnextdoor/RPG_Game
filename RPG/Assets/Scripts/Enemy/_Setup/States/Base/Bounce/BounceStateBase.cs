using System.Collections.Generic;
using UnityEngine;

public class BounceStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private static readonly List<StateSound> Empty = new();

    protected readonly List<StateSound> enterSounds;
    protected readonly List<StateSound> exitSounds;

    protected readonly BounceMovementHandler<TEnemy> movementHandler;

    #region Bounce runtime

    #region Enum
    public enum BounceSurface
    {
        Floor,
        RightWall,
        Ceiling,
        LeftWall
    }

    public enum BouncePhase
    {
        Attach,
        Launch,
        Airborne,
        Complete
    }

    public enum BounceJumpType
    {
        Normal,
        FloorToCeiling
    }

    public enum BounceJumpSide
    {
        Left,
        Right
    }

    public enum BounceAirMoveType
    {
        None,
        Arc,
        Curve,
        Edge
    }

    

    #endregion

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

    #region Bounce phases

    protected virtual void AttachPhase()
    {
        SurfacePhase();

        bool isAligned = movementHandler.IsFeetAlignedToSurface();
        if (!isAligned)
            return;

        hasLandingHit = false;
        movementHandler.ClearIgnoredLaunchSurfaces();
        currentPhase = BouncePhase.Complete;
        OnAttachComplete();
    }

    protected virtual void SurfacePhase()
    {
        SetAttachedGravity();
        enemy.SetZeroVelocity();
        movementHandler.RotateFeetToSurface();
    }

    protected virtual void LaunchPhase()
    {
        SetAirborneGravity();

        movementHandler.CacheIgnoredSecondarySurface();

        Vector2 launchDirection = currentSurfaceNormal.normalized;
        Vector2 targetPosition = rb.position + launchDirection * (movementHandler.MoveSpeed() * Time.deltaTime);

        movementHandler.ApplyBounceMovement(targetPosition, launchDirection);

        if (!movementHandler.HasLeftCurrentSurface())
            return;

        currentPhase = BouncePhase.Airborne;
    }

    protected virtual void AirbornePhase()
    {
        movementHandler.MoveAirborne();

        movementHandler.UpdateIgnoredSecondarySurface();

        if (movementHandler.TryFindLandingSurface(out Vector2 hitPoint, out Vector2 hitNormal))
        {
            landingPoint = hitPoint;
            hasLandingHit = true;

            currentSurface = movementHandler.ClassifySurface(hitNormal);
            movementHandler.AlignToSurfaceNormal(hitNormal);

            currentPhase = BouncePhase.Attach;
            return;
        }
    }

    protected virtual void OnAttachComplete()
    {
    }
    protected virtual void CompletePhase()
    {
    }

    #endregion

    #region Jump planning

    protected virtual void PlanJump()
    {
        plannedJumpSide = GetJumpSide();
        plannedJumpDirection = GetLocalJumpDirection(plannedJumpSide).normalized;
        plannedJumpDistance = GetJumpDistance(plannedJumpSide);
        plannedJumpType = GetWeightedJumpType();
        plannedJumpHeight = GetJumpHeight(plannedJumpType);

        launchSurface = currentSurface;
        plannedArcAwayAxis = movementHandler.SurfaceNormal(launchSurface).normalized;
        plannedArcStartZ = movementHandler.GetSurfaceTargetZ(launchSurface);

        plannedAirMoveType = GetAirMoveType(launchSurface, plannedJumpType);

        movementHandler.ResetMovementRuntimeForNewPlan(enemy.transform.position);

        //Debug.Log($"{enemy.name} PlanJump | Surface={launchSurface} | JumpType={plannedJumpType} | Side={plannedJumpSide} | AirMoveType={plannedAirMoveType} | Distance={plannedJumpDistance} | Height={plannedJumpHeight} | Direction={plannedJumpDirection}");
    }

    #region Jump helpers

    #region Direction
    protected virtual BounceJumpSide GetJumpSide()
    {
        if (IsAtLeftEdge())
            return BounceJumpSide.Right;

        if (IsAtRightEdge())
            return BounceJumpSide.Left;

        return UnityEngine.Random.value < 0.5f
            ? BounceJumpSide.Left
            : BounceJumpSide.Right;
    }

    private Vector2 GetLocalJumpDirection(BounceJumpSide jumpSide)
    {
        switch (currentSurface)
        {
            case BounceSurface.Floor:
                return jumpSide == BounceJumpSide.Left
                    ? Vector2.left
                    : Vector2.right;

            case BounceSurface.RightWall:
                return jumpSide == BounceJumpSide.Left
                    ? Vector2.down
                    : Vector2.up;

            case BounceSurface.Ceiling:
                return jumpSide == BounceJumpSide.Left
                    ? Vector2.right
                    : Vector2.left;

            case BounceSurface.LeftWall:
                return jumpSide == BounceJumpSide.Left
                    ? Vector2.up
                    : Vector2.down;
        }

        return jumpSide == BounceJumpSide.Left
            ? Vector2.left
            : Vector2.right;
    }

    protected virtual bool IsAtLeftEdge()
    {
        return IsJumpSideAtPatrolEdge(BounceJumpSide.Left) || IsJumpSideBlockedByWall(BounceJumpSide.Left);
    }
    protected virtual bool IsAtRightEdge()
    {
        return IsJumpSideAtPatrolEdge(BounceJumpSide.Right) || IsJumpSideBlockedByWall(BounceJumpSide.Right);
    }
    private bool IsJumpSideAtPatrolEdge(BounceJumpSide jumpSide)
    {
        float edgePadding = GetEnemyWidth() * 0.5f;
        Enemy.PatrolPositionInfo patrolInfo = enemy.GetPatrolPositionInfo(enemy.transform.position, edgePadding);

        Vector2 jumpDirection = GetLocalJumpDirection(jumpSide).normalized;
        if (jumpDirection.sqrMagnitude <= 0.0001f)
            return false;

        bool pushingIntoLeft = patrolInfo.nearLeft && jumpDirection.x < -0.01f;
        bool pushingIntoRight = patrolInfo.nearRight && jumpDirection.x > 0.01f;
        bool pushingIntoBottom = patrolInfo.nearBottom && jumpDirection.y < -0.01f;
        bool pushingIntoTop = patrolInfo.nearTop && jumpDirection.y > 0.01f;

        return pushingIntoLeft || pushingIntoRight || pushingIntoBottom || pushingIntoTop;
    }
    private bool IsJumpSideBlockedByWall(BounceJumpSide jumpSide)
    {
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null)
            return false;

        Vector2 direction = GetLocalJumpDirection(jumpSide).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        Vector2 origin = GetJumpSideProbeOrigin(col, direction);
        float distance = GetJumpSideProbeDistance(col);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, enemy.GetWhatIsGround());
        return hit.collider != null;
    }
    private Vector2 GetJumpSideProbeOrigin(Collider2D col, Vector2 direction)
    {
        direction.Normalize();

        Bounds bounds = col.bounds;
        Vector2 center = bounds.center;
        Vector2 extents = bounds.extents;

        float reach = Vector2.Dot(extents, new Vector2(Mathf.Abs(direction.x), Mathf.Abs(direction.y)));
        reach = Mathf.Max(0.01f, reach);

        float skin = Mathf.Max(0.005f, Physics2D.defaultContactOffset);
        return center + direction * (reach + skin);
    }
    private float GetJumpSideProbeDistance(Collider2D col)
    {
        Bounds bounds = col.bounds;
        float sizeAlongDirection = Mathf.Max(bounds.size.x, bounds.size.y);
        return Mathf.Max(0.05f, sizeAlongDirection * 0.5f);
    }
    #endregion
    protected virtual BounceJumpType GetWeightedJumpType()
    {
        if (currentSurface != BounceSurface.Floor)
            return BounceJumpType.Normal;

        if (!TryGetGroundAbovePlannedJump(GetFloorToCeilingSpan(), out _))
            return BounceJumpType.Normal;

        int normalWeight = 75;
        int floorToCeilingWeight = 25;

        int totalWeight = normalWeight + floorToCeilingWeight;
        int roll = UnityEngine.Random.Range(0, totalWeight);

        if (roll < normalWeight)
            return BounceJumpType.Normal;

        return BounceJumpType.FloorToCeiling;
    }

    private BounceAirMoveType GetAirMoveType(BounceSurface surface, BounceJumpType jumpType)
    {
        if (jumpType == BounceJumpType.FloorToCeiling)
            return BounceAirMoveType.Curve;

        switch (surface)
        {
            case BounceSurface.Floor:
                return BounceAirMoveType.Arc;

            case BounceSurface.RightWall:
            case BounceSurface.LeftWall:
            case BounceSurface.Ceiling:
            default:
                return BounceAirMoveType.Curve;
        }
    }

    #region Distance
    protected virtual float GetJumpDistance(BounceJumpSide jumpSide)
    {
        GetJumpDistanceRange(jumpSide, out float minDistance, out float maxDistance);
        return UnityEngine.Random.Range(minDistance, maxDistance);
    }

    protected virtual void GetJumpDistanceRange(BounceJumpSide jumpSide, out float minDistance, out float maxDistance)
    {
        float enemyWidth = GetEnemyWidth();

        bool useLongMin =
            currentSurface == BounceSurface.LeftWall ||
            currentSurface == BounceSurface.RightWall ||
            plannedJumpType == BounceJumpType.FloorToCeiling;

        if (useLongMin)
            minDistance = enemyWidth * 1.5f;
        else
            minDistance = enemyWidth * 0.5f;

        maxDistance = GetJumpMaxDistance(jumpSide);

        if (maxDistance < minDistance)
            minDistance = maxDistance;
    }

    protected virtual float GetJumpMaxDistance(BounceJumpSide jumpSide)
    {
        float enemyWidth = GetEnemyWidth();
        return enemyWidth * Mathf.Max(1, enemy.bounceJumpDistanceMultiplier);
    }

    protected virtual float GetEnemyWidth()
    {
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col != null)
            return Mathf.Max(0.1f, col.bounds.size.x);

        return 1f;
    }

    #endregion

    #region Height
    protected virtual float GetJumpHeight(BounceJumpType jumpType)
    {
        GetJumpHeightRange(jumpType, out float minHeight, out float maxHeight);
        return UnityEngine.Random.Range(minHeight, maxHeight);
    }

    protected virtual void GetJumpHeightRange(BounceJumpType jumpType, out float minHeight, out float maxHeight)
    {
        minHeight = GetPlayerHeight();
        maxHeight = GetJumpMaxHeight(jumpType, minHeight);

        if (maxHeight < minHeight)
            minHeight = maxHeight;
    }

    private float GetPlayerHeight()
    {
        var player = PlayerUtils.GetPlayerSafe();
        if (player == null)
            return 1f;

        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null)
            return Mathf.Max(0.1f, col.bounds.size.y);

        return 1f;
    }

    protected virtual float GetJumpMaxHeight(BounceJumpType jumpType, float playerHeight)
    {
        switch (currentSurface)
        {
            case BounceSurface.Floor:
                return GetFloorJumpMaxHeight(jumpType, playerHeight);

            case BounceSurface.RightWall:
            case BounceSurface.LeftWall:
            case BounceSurface.Ceiling:
                return playerHeight;
        }

        return playerHeight;
    }

    protected virtual float GetFloorJumpMaxHeight(BounceJumpType jumpType, float playerHeight)
    {
        float patrolSpan = GetFloorToCeilingSpan();

        if (jumpType == BounceJumpType.FloorToCeiling)
            return Mathf.Max(playerHeight, patrolSpan);

        if (TryGetGroundAbovePlannedJump(patrolSpan, out RaycastHit2D hit))
            patrolSpan = hit.distance;

        patrolSpan -= GetEnemyHeight() + GetSurfaceClearance();

        return Mathf.Max(playerHeight, patrolSpan);
    }

    private bool TryGetGroundAbovePlannedJump(float checkHeight, out RaycastHit2D hit)
    {
        hit = default;

        if (checkHeight <= 0f)
            return false;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null)
            return false;

        Bounds bounds = col.bounds;

        float skin = Mathf.Max(0.005f, Physics2D.defaultContactOffset);
        float checkDistance = checkHeight - GetEnemyHeight() - skin;

        if (checkDistance <= 0f)
            return false;

        Vector2 origin = new Vector2(bounds.center.x, bounds.max.y + skin);

        hit = Physics2D.Raycast(
            origin,
            Vector2.up,
            checkDistance,
            enemy.GetWhatIsGround()
        );

        return hit.collider != null;
    }

    private float GetEnemyHeight()
    {
        Collider2D col = enemy.GetComponent<Collider2D>();

        if (col != null)
            return Mathf.Max(0.1f, col.bounds.size.y);

        return 1f;
    }

    private float GetSurfaceClearance()
    {
        return 0.03f;
    }

    private float GetFloorToCeilingSpan()
    {
        return Mathf.Max(0.1f, enemy.patrolAreaSize.y);
    }

    #endregion

    #endregion

    #endregion

    #region Gravity
    private void CacheDefaultGravity()
    {
        if (gravityCached)
            return;

        savedDefaultGravity = rb.gravityScale;
        gravityCached = true;
    }

    private void SetAttachedGravity()
    {
        rb.gravityScale = 0f;
    }

    private void SetAirborneGravity()
    {
        rb.gravityScale = savedDefaultGravity;
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