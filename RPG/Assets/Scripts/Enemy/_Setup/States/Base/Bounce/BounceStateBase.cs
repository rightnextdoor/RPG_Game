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

    public enum BounceEdgePoint
    {
        None,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
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

    protected float surfaceTargetZ;
    protected float surfaceRotateVelocity;
    protected const float SURFACE_ALIGN_TIME = 0.06f;
    protected const float SURFACE_ALIGN_EPS_DEG = 0.75f;

    #endregion

    #region Phase Settings
    protected bool isAttached;
    protected bool isAirborne;
    protected float savedDefaultGravity;
    protected bool gravityCached;

    protected Vector2 landingPoint;
    protected bool hasLandingHit;

    protected float curveTravelDistance;
    protected bool curveReachedEnd;

    protected const float AIRBORNE_ROTATE_SPEED = 720f;
    protected const float LANDING_NORMAL_MIN_DOT = 0.1f;
    #endregion

    #region Plan Settings
    protected BounceJumpType plannedJumpType = BounceJumpType.Normal;
    protected BounceJumpSide plannedJumpSide = BounceJumpSide.Right;
    protected BounceAirMoveType plannedAirMoveType = BounceAirMoveType.None;

    protected Vector2 plannedJumpDirection = Vector2.right;
    protected float plannedJumpDistance;
    protected float plannedJumpHeight;
    protected BounceSurface launchSurface = BounceSurface.Floor;

    protected Vector2 airStartPosition;
    protected float airTravelDistance;
    protected bool airMoveInitialized;
    protected bool arcReachedEnd;

    protected Vector2 plannedArcAwayAxis = Vector2.up;
    protected float plannedArcStartZ;

    protected bool edgeBouncePlanBuilt;
    protected Vector2 edgeBounceHitDirection;
    protected Vector2 edgeBounceSurfaceNormal;

    protected bool edgeSurfaceReleaseActive;
    protected bool edgeSurfaceReleased;

    protected BounceEdgePoint edgeBouncePoint = BounceEdgePoint.None;
    #endregion

    #endregion

    protected Vector2 edgeBounceProbeHitPoint;

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
    internal BounceSurface MovementCurrentSurface => currentSurface;
    internal BounceAirMoveType MovementPlannedAirMoveType => plannedAirMoveType;
    internal bool MovementEdgeBouncePlanBuilt => edgeBouncePlanBuilt;

    internal BounceEdgePoint MovementEdgeBouncePoint
    {
        get => edgeBouncePoint;
        set => edgeBouncePoint = value;
    }

    internal Vector2 MovementEdgeBounceProbeHitPoint
    {
        get => edgeBounceProbeHitPoint;
        set => edgeBounceProbeHitPoint = value;
    }

    internal BounceSurface MovementClassifySurface(Vector2 normal)
    {
        return ClassifySurface(normal);
    }

    internal Vector2 MovementSurfaceNormal(BounceSurface surface)
    {
        return SurfaceNormal(surface);
    }

    internal Vector2 MovementCurrentSurfaceNormal => currentSurfaceNormal;

    internal float MovementMoveSpeed()
    {
        return MoveSpeed();
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

    internal BounceSurface MovementResolveSurfaceFromProbeScan(
        BounceSurface rawSurface,
        BounceMovementHandler<TEnemy>.LandingProbeScan scan
    )
    {
        return ResolveSurfaceFromProbeScan(rawSurface, scan);
    }

    internal void MovementRequestEdgeBouncePlan(
        BounceMovementHandler<TEnemy>.LandingProbeScan scan,
        Vector2 rawNormal,
        bool forceRebuild
    )
    {
        RequestEdgeBouncePlan(scan, rawNormal, forceRebuild);
    }

    #endregion

    #region Bounce phases

    protected virtual void AttachPhase()
    {
        SurfacePhase();

        bool isAligned = IsFeetAlignedToSurface();
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
        RotateFeetToSurface();
    }

    protected virtual void LaunchPhase()
    {
        SetAirborneGravity();

        movementHandler.CacheIgnoredSecondarySurface();

        Vector2 launchDirection = currentSurfaceNormal.normalized;
        Vector2 targetPosition = rb.position + launchDirection * (MoveSpeed() * Time.deltaTime);

        ApplyBounceMovement(targetPosition, launchDirection);

        if (!HasLeftCurrentSurface())
            return;

        currentPhase = BouncePhase.Airborne;
    }

    protected virtual void AirbornePhase()
    {
        MoveAirborne();

        movementHandler.UpdateIgnoredSecondarySurface();

        if (movementHandler.TryFindLandingSurface(out Vector2 hitPoint, out Vector2 hitNormal))
        {
            landingPoint = hitPoint;
            hasLandingHit = true;

            currentSurface = ClassifySurface(hitNormal);
            AlignToSurfaceNormal(hitNormal);

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
        plannedArcAwayAxis = SurfaceNormal(launchSurface).normalized;
        plannedArcStartZ = GetSurfaceTargetZ(launchSurface);

        plannedAirMoveType = GetAirMoveType(launchSurface, plannedJumpType);

        airStartPosition = enemy.transform.position;
        airTravelDistance = 0f;
        curveTravelDistance = 0f;
        airMoveInitialized = false;
        arcReachedEnd = false;
        curveReachedEnd = false;

        edgeBouncePlanBuilt = false;
        edgeBounceHitDirection = Vector2.zero;
        edgeBounceSurfaceNormal = Vector2.zero;
        edgeBouncePoint = BounceEdgePoint.None;
        edgeBounceProbeHitPoint = Vector2.zero;

        edgeSurfaceReleaseActive = false;
        edgeSurfaceReleased = false;

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

    #region Movement
    protected virtual float MoveSpeed()
    {
        return enemy.moveSpeed * enemy.moveSpeedMultiplier;
    }

    private void ApplyBounceMovement(Vector2 targetPosition, Vector2 pathDirection)
    {
        if (pathDirection.sqrMagnitude > 0.0001f)
            movementHandler.LandingIgnoreFailSafePathDirection = pathDirection.normalized;

        rb.MovePosition(targetPosition);
        rb.linearVelocity = pathDirection.normalized * MoveSpeed();
    }

    private void MoveAirborne()
    {
        switch (plannedAirMoveType)
        {
            case BounceAirMoveType.Arc:
                MoveAirborneArc();
                break;

            case BounceAirMoveType.Curve:
                MoveAirborneCurve();
                break;

            case BounceAirMoveType.Edge:
                MoveAirborneEdge();
                break;
        }
    }

    private void MoveAirborneArc()
    {
        float moveSpeed = MoveSpeed();
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        if (!airMoveInitialized)
        {
            airStartPosition = rb.position;
            airTravelDistance = 0f;
            airMoveInitialized = true;
            arcReachedEnd = false;
        }

        Vector2 alongAxis = plannedJumpDirection.sqrMagnitude > 0.0001f
            ? plannedJumpDirection.normalized
            : Vector2.right;

        Vector2 awayAxis = plannedArcAwayAxis.sqrMagnitude > 0.0001f
            ? plannedArcAwayAxis.normalized
            : SurfaceNormal(launchSurface).normalized;

        MoveAirborneArcTravel(moveSpeed, deltaTime, alongAxis, awayAxis);
    }

    private void MoveAirborneCurve()
    {
        switch (launchSurface)
        {
            case BounceSurface.Floor:
                MoveAirborneCurveFromFloor();
                break;

            case BounceSurface.RightWall:
            case BounceSurface.LeftWall:
                MoveAirborneCurveFromWall();
                break;

            case BounceSurface.Ceiling:
                MoveAirborneCurveFromCeiling();
                break;
        }
    }

    private void MoveAirborneEdge()
    {
        if (!edgeBouncePlanBuilt)
            BuildEdgeBouncePlan();

        if (!CheckEdgeSurfaceRelease())
            return;

        MoveAirborneCurveFromEdge();
    }

    #region Helpers
    #region Edge

    #region Plan
    private void RequestEdgeBouncePlan(
    BounceMovementHandler<TEnemy>.LandingProbeScan scan,
    Vector2 rawNormal,
    bool forceRebuild
)
    {
        if (plannedAirMoveType != BounceAirMoveType.Edge)
            edgeBouncePlanBuilt = false;

        if (forceRebuild)
            edgeBouncePlanBuilt = false;

        if (edgeBouncePlanBuilt)
            return;

        edgeBounceHitDirection = GetEdgeBounceHitDirection(scan);

        edgeBounceSurfaceNormal = rawNormal.sqrMagnitude > 0.0001f
            ? rawNormal.normalized
            : Vector2.zero;

        plannedAirMoveType = BounceAirMoveType.Edge;

    }

    private void BuildEdgeBouncePlan()
    {
        PrepareEdgeSurfaceContact();

        LaunchFromEdge();

        edgeBouncePlanBuilt = true;
    }

    private void PrepareEdgeSurfaceContact()
    {
        SetAttachedGravity();

        enemy.SetZeroVelocity();

        RotateFeetToEdgePoint();
    }

    #endregion

    #region Move
    private void MoveAirborneCurveFromEdge()
    {
        if (!airMoveInitialized)
        {
            airStartPosition = rb.position;
            airTravelDistance = 0f;
            curveTravelDistance = 0f;
            airMoveInitialized = true;
            curveReachedEnd = false;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        Vector2 edgeDirection = plannedJumpDirection.sqrMagnitude > 0.0001f
            ? plannedJumpDirection.normalized
            : plannedArcAwayAxis.normalized;

        float xDirection = Mathf.Sign(edgeDirection.x);
        if (Mathf.Abs(xDirection) <= 0.0001f)
            xDirection = plannedJumpSide == BounceJumpSide.Right ? 1f : -1f;

        float bendDirection = Mathf.Sign(edgeDirection.y);
        if (Mathf.Abs(bendDirection) <= 0.0001f)
            bendDirection = movementHandler.LandingIgnoreFailSafePathDirection.y >= 0f ? 1f : -1f;

        float totalDistance = Mathf.Max(0.001f, plannedJumpDistance);
        float targetBend = Mathf.Max(0.001f, GetFloorToCeilingSpan());

        float slope = GetCurveSlope(curveTravelDistance, totalDistance, targetBend);

        float xStep = MoveSpeed() * deltaTime / Mathf.Sqrt(1f + (slope * slope));
        curveTravelDistance += xStep;

        float progress = Mathf.Clamp01(curveTravelDistance / totalDistance);
        float bendAmount = GetCurveRise(progress, targetBend);

        Vector2 alongAxis = new Vector2(xDirection, 0f);
        Vector2 bendAxis = new Vector2(0f, bendDirection);

        Vector2 targetPosition = airStartPosition
                               + alongAxis * curveTravelDistance
                               + bendAxis * bendAmount;

        float tangentSlope = GetCurveSlope(curveTravelDistance, totalDistance, targetBend);
        Vector2 pathDirection = (alongAxis + bendAxis * tangentSlope).normalized;

        MoveAirborneCurveTravel(targetPosition, pathDirection);
    }

    #endregion

    #region Helpers

    #region Feet Rotation
    private void RotateFeetToEdgePoint()
    {
        Vector2 snapSurfaceNormal = edgeBounceSurfaceNormal.sqrMagnitude > 0.0001f
            ? edgeBounceSurfaceNormal.normalized
            : plannedArcAwayAxis.normalized;

        if (snapSurfaceNormal.sqrMagnitude <= 0.0001f)
            snapSurfaceNormal = SurfaceNormal(launchSurface).normalized;

        Vector2 feetTarget = -snapSurfaceNormal;
        float edgeSurfaceZ = Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = edgeSurfaceZ;
        enemy.transform.eulerAngles = eulerAngles;

        surfaceRotateVelocity = 0f;
        plannedArcStartZ = edgeSurfaceZ;

        if (ValidateEdgeFeetRotation())
            return;

        feetTarget = snapSurfaceNormal;
        edgeSurfaceZ = Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));

        eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = edgeSurfaceZ;
        enemy.transform.eulerAngles = eulerAngles;

        surfaceRotateVelocity = 0f;
        plannedArcStartZ = edgeSurfaceZ;

        ValidateEdgeFeetRotation();
    }

    private bool ValidateEdgeFeetRotation()
    {
        if (edgeBounceProbeHitPoint.sqrMagnitude <= 0.0001f)
            return true;

        Vector3 localEdgePoint3 = enemy.transform.InverseTransformPoint(edgeBounceProbeHitPoint);
        Vector2 localEdgePoint = new Vector2(localEdgePoint3.x, localEdgePoint3.y);

        bool edgePointIsOnFeetSide = localEdgePoint.y <= 0f;

        return edgePointIsOnFeetSide;
    }
    #endregion

    #region Launch
    private void LaunchFromEdge()
    {
        SetAirborneGravity();

        Vector2 incomingDirection = GetEdgeIncomingDirection();
        Vector2 awayAxis = GetEdgeBounceAwayAxis(incomingDirection);

        plannedArcAwayAxis = awayAxis.sqrMagnitude > 0.0001f
            ? awayAxis.normalized
            : SurfaceNormal(launchSurface).normalized;

        Vector2 beforeLaunchPosition = rb.position;
        Vector2 afterLaunchPosition = MoveEnemySlightlyOffEdge(plannedArcAwayAxis);

        StoreEdgeLaunchDirection(beforeLaunchPosition, afterLaunchPosition, plannedArcAwayAxis);

        ResetEdgeCurveTravelFromLaunch(afterLaunchPosition);

        StartEdgeSurfaceRelease();
    }
    private void ResetEdgeCurveTravelFromLaunch(Vector2 launchStartPosition)
    {
        airStartPosition = launchStartPosition;
        airTravelDistance = 0f;
        curveTravelDistance = 0f;
        airMoveInitialized = true;
        arcReachedEnd = false;
        curveReachedEnd = false;
    }
    private void StoreEdgeLaunchDirection(Vector2 beforeLaunchPosition, Vector2 afterLaunchPosition, Vector2 fallbackDirection)
    {
        Vector2 launchDirection = afterLaunchPosition - beforeLaunchPosition;

        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = fallbackDirection;

        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = plannedArcAwayAxis;

        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = SurfaceNormal(launchSurface);

        plannedJumpDirection = launchDirection.normalized;

        movementHandler.LandingIgnoreFailSafePathDirection = plannedJumpDirection;

        float originalDistance = Mathf.Max(0.001f, plannedJumpDistance);
        float maxEdgeDistance = originalDistance * 0.5f;
        float minEdgeDistance = maxEdgeDistance * 0.75f;

        plannedJumpDistance = UnityEngine.Random.Range(minEdgeDistance, maxEdgeDistance);

        Vector2 localRight = new Vector2(enemy.transform.right.x, enemy.transform.right.y).normalized;
        float localSideValue = Vector2.Dot(plannedJumpDirection, localRight);

        plannedJumpSide = localSideValue < 0f
            ? BounceJumpSide.Left
            : BounceJumpSide.Right;
    }
    private void StartEdgeSurfaceRelease()
    {
        movementHandler.StartEdgeSurfaceRelease();

        edgeSurfaceReleaseActive = true;
        edgeSurfaceReleased = false;
    }

    private bool CheckEdgeSurfaceRelease()
    {
        if (edgeSurfaceReleased)
            return true;

        movementHandler.UpdateIgnoredSecondarySurface();

        edgeSurfaceReleaseActive = false;
        edgeSurfaceReleased = true;

        return true;
    }
    #endregion

    #region Move
    private Vector2 MoveEnemySlightlyOffEdge(Vector2 awayAxis)
    {
        Vector2 moveDirection = awayAxis.sqrMagnitude > 0.0001f
            ? awayAxis.normalized
            : GetEdgeIncomingDirection();

        float moveAmount = Mathf.Max(GetSurfaceClearance(), MoveSpeed() * Time.deltaTime);
        Vector2 targetPosition = rb.position + moveDirection * moveAmount;

        ApplyBounceMovement(targetPosition, moveDirection);

        return targetPosition;
    }
    private Vector2 GetEdgeBounceAwayAxis(Vector2 incomingDirection)
    {
        Vector2 awayAxis = edgeBounceSurfaceNormal;

        if (awayAxis.sqrMagnitude <= 0.0001f && edgeBounceHitDirection.sqrMagnitude > 0.0001f)
            awayAxis = -edgeBounceHitDirection.normalized;

        if (awayAxis.sqrMagnitude <= 0.0001f)
        {
            awayAxis = plannedArcAwayAxis.sqrMagnitude > 0.0001f
                ? plannedArcAwayAxis.normalized
                : SurfaceNormal(launchSurface).normalized;
        }

        awayAxis.Normalize();

        if (incomingDirection.sqrMagnitude > 0.0001f && Vector2.Dot(incomingDirection.normalized, awayAxis) > 0f)
            awayAxis = -awayAxis;

        return awayAxis.normalized;
    }
    private Vector2 GetEdgeIncomingDirection()
    {
        Vector2 incomingDirection = rb.linearVelocity;

        if (incomingDirection.sqrMagnitude > 0.0001f)
            return incomingDirection.normalized;

        incomingDirection = movementHandler.LandingIgnoreFailSafePathDirection;

        if (incomingDirection.sqrMagnitude > 0.0001f)
            return incomingDirection.normalized;

        incomingDirection = plannedJumpDirection + plannedArcAwayAxis;

        if (incomingDirection.sqrMagnitude > 0.0001f)
            return incomingDirection.normalized;

        return SurfaceNormal(launchSurface).normalized;
    }
    private Vector2 GetEdgeBounceHitDirection(BounceMovementHandler<TEnemy>.LandingProbeScan scan)
    {
        Vector2 direction = Vector2.zero;

        if (scan.upLeft)
            direction += (Vector2.up + Vector2.left).normalized;

        if (scan.upRight)
            direction += (Vector2.up + Vector2.right).normalized;

        if (scan.downLeft)
            direction += (Vector2.down + Vector2.left).normalized;

        if (scan.downRight)
            direction += (Vector2.down + Vector2.right).normalized;

        if (direction.sqrMagnitude > 0.0001f)
            return direction.normalized;

        return GetEdgeIncomingDirection();
    }
    #endregion

    #endregion

    #endregion

    #region Arc
    private void MoveAirborneArcTravel(float moveSpeed, float deltaTime, Vector2 alongAxis, Vector2 awayAxis)
    {
        if (!arcReachedEnd)
        {
            float totalDistance = Mathf.Max(0.001f, plannedJumpDistance);

            float slope = GetArcHeightSlope(airTravelDistance, totalDistance, plannedJumpHeight);
            float alongStep = moveSpeed * deltaTime / Mathf.Sqrt(1f + (slope * slope));

            airTravelDistance = Mathf.Min(totalDistance, airTravelDistance + alongStep);

            float progress = Mathf.Clamp01(airTravelDistance / totalDistance);
            float heightAmount = Mathf.Sin(progress * Mathf.PI) * plannedJumpHeight;

            Vector2 targetPosition = airStartPosition
                                   + alongAxis * airTravelDistance
                                   + awayAxis * heightAmount;

            float tangentSlope = GetArcHeightSlope(airTravelDistance, totalDistance, plannedJumpHeight);
            Vector2 pathDirection = (alongAxis + awayAxis * tangentSlope).normalized;

            ApplyBounceMovement(targetPosition, pathDirection);
            RotateAirborne(pathDirection);

            if (progress >= 1f)
                arcReachedEnd = true;

            return;
        }

        rb.linearVelocity = rb.linearVelocity;
    }

    private float GetArcHeightSlope(float travelDistance, float totalDistance, float maxHeight)
    {
        float progress = Mathf.Clamp01(travelDistance / Mathf.Max(0.001f, totalDistance));
        return (Mathf.PI * maxHeight / Mathf.Max(0.001f, totalDistance)) * Mathf.Cos(progress * Mathf.PI);
    }

    #endregion

    #region Curve
    private void MoveAirborneCurveFromFloor()
    {
        if (!airMoveInitialized)
        {
            airStartPosition = rb.position;
            airTravelDistance = 0f;
            curveTravelDistance = 0f;
            airMoveInitialized = true;
            curveReachedEnd = false;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        if (!curveReachedEnd)
        {
            Vector2 endPoint = GetCurveEndPoint();

            float totalDistance = Mathf.Max(0.001f, Mathf.Abs(endPoint.x - airStartPosition.x));
            float xDirection = Mathf.Sign(endPoint.x - airStartPosition.x);

            if (Mathf.Abs(xDirection) <= 0.0001f)
                xDirection = plannedJumpDirection.x >= 0f ? 1f : -1f;

            float targetRise = endPoint.y - airStartPosition.y;
            float slope = GetCurveSlope(curveTravelDistance, totalDistance, targetRise);

            float xStep = MoveSpeed() * deltaTime / Mathf.Sqrt(1f + (slope * slope));
            curveTravelDistance = Mathf.Min(totalDistance, curveTravelDistance + xStep);

            float progress = Mathf.Clamp01(curveTravelDistance / totalDistance);
            float riseAmount = GetCurveRise(progress, targetRise);

            Vector2 alongAxis = new Vector2(xDirection, 0f);
            Vector2 riseAxis = Vector2.up;

            Vector2 targetPosition = airStartPosition
                                   + alongAxis * curveTravelDistance
                                   + riseAxis * riseAmount;

            float tangentSlope = GetCurveSlope(curveTravelDistance, totalDistance, targetRise);
            Vector2 pathDirection = (alongAxis + riseAxis * tangentSlope).normalized;

            MoveAirborneCurveTravel(targetPosition, pathDirection);

            if (progress >= 1f)
                curveReachedEnd = true;

            return;
        }

        Vector2 upDirection = Vector2.up;
        Vector2 continuePosition = rb.position + upDirection * (MoveSpeed() * deltaTime);

        MoveAirborneCurveTravel(continuePosition, upDirection);
    }

    private void MoveAirborneCurveFromWall()
    {
        if (!airMoveInitialized)
        {
            airStartPosition = rb.position;
            airTravelDistance = 0f;
            curveTravelDistance = 0f;
            airMoveInitialized = true;
            curveReachedEnd = false;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        float verticalDistance = Mathf.Max(0.001f, GetFloorToCeilingSpan());

        float verticalDirection = Mathf.Sign(plannedJumpDirection.y);
        if (Mathf.Abs(verticalDirection) <= 0.0001f)
            verticalDirection = plannedJumpSide == BounceJumpSide.Right ? 1f : -1f;

        float outwardDirection = currentSurface == BounceSurface.RightWall ? -1f : 1f;
        float outwardDistance = Mathf.Max(0.001f, plannedJumpDistance);

        float slope = GetCurveSlope(curveTravelDistance, verticalDistance, outwardDistance);

        float yStep = MoveSpeed() * deltaTime / Mathf.Sqrt(1f + (slope * slope));
        curveTravelDistance += yStep;

        float progress = Mathf.Clamp01(curveTravelDistance / verticalDistance);
        float outwardAmount = GetCurveRise(progress, outwardDistance);

        Vector2 alongAxis = new Vector2(0f, verticalDirection);
        Vector2 riseAxis = new Vector2(outwardDirection, 0f);

        Vector2 targetPosition = airStartPosition
                               + alongAxis * curveTravelDistance
                               + riseAxis * outwardAmount;

        float tangentSlope = GetCurveSlope(curveTravelDistance, verticalDistance, outwardDistance);
        Vector2 pathDirection = (alongAxis + riseAxis * tangentSlope).normalized;

        MoveAirborneCurveTravel(targetPosition, pathDirection);
    }

    private void MoveAirborneCurveFromCeiling()
    {
        if (!airMoveInitialized)
        {
            airStartPosition = rb.position;
            airTravelDistance = 0f;
            curveTravelDistance = 0f;
            airMoveInitialized = true;
            curveReachedEnd = false;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        if (!curveReachedEnd)
        {
            Vector2 floorEndPoint = GetCurveEndPoint();

            float totalDistance = Mathf.Max(0.001f, Mathf.Abs(floorEndPoint.x - airStartPosition.x));
            float xDirection = Mathf.Sign(floorEndPoint.x - airStartPosition.x);

            if (Mathf.Abs(xDirection) <= 0.0001f)
                xDirection = plannedJumpDirection.x >= 0f ? 1f : -1f;

            float targetDrop = Mathf.Abs(floorEndPoint.y - airStartPosition.y);
            float slope = GetCurveSlope(curveTravelDistance, totalDistance, targetDrop);

            float xStep = MoveSpeed() * deltaTime / Mathf.Sqrt(1f + (slope * slope));
            curveTravelDistance = Mathf.Min(totalDistance, curveTravelDistance + xStep);

            float progress = Mathf.Clamp01(curveTravelDistance / totalDistance);
            float dropAmount = GetCurveRise(progress, targetDrop);

            Vector2 alongAxis = new Vector2(xDirection, 0f);
            Vector2 dropAxis = Vector2.down;

            Vector2 targetPosition = airStartPosition
                                   + alongAxis * curveTravelDistance
                                   + dropAxis * dropAmount;

            float tangentSlope = GetCurveSlope(curveTravelDistance, totalDistance, targetDrop);
            Vector2 pathDirection = (alongAxis + dropAxis * tangentSlope).normalized;

            MoveAirborneCurveTravel(targetPosition, pathDirection);

            if (progress >= 1f)
                curveReachedEnd = true;

            return;
        }

        float fallX = plannedJumpDirection.x;
        if (Mathf.Abs(fallX) <= 0.0001f)
            fallX = plannedJumpSide == BounceJumpSide.Right ? 1f : -1f;

        Vector2 fallDirection = new Vector2(fallX * 0.35f, -1f).normalized;
        Vector2 continuePosition = rb.position + (fallDirection * (MoveSpeed() * deltaTime));

        MoveAirborneCurveTravel(continuePosition, fallDirection);
    }

    #region Helpers
    protected virtual void MoveAirborneCurveTravel(Vector2 targetPosition, Vector2 pathDirection)
    {
        ApplyBounceMovement(targetPosition, pathDirection);
        RotateAirborne(pathDirection);
    }
    protected virtual float GetCurveSlope(float travelDistance, float totalDistance, float targetRise)
    {
        float safeDistance = Mathf.Max(0.001f, totalDistance);
        float progress = Mathf.Clamp01(travelDistance / safeDistance);

        return (targetRise * (Mathf.PI * 0.5f) / safeDistance) * Mathf.Cos(progress * Mathf.PI * 0.5f);
    }

    protected virtual Vector2 GetCurveEndPoint()
    {
        float targetX = airStartPosition.x + (plannedJumpDirection.x * plannedJumpDistance);
        float targetY = airStartPosition.y + GetFloorToCeilingSpan();

        return new Vector2(targetX, targetY);
    }

    protected virtual float GetCurveRise(float progress, float targetRise)
    {
        targetRise = Mathf.Max(0.001f, targetRise);
        return Mathf.Sin(progress * Mathf.PI * 0.5f) * targetRise;
    }


    #endregion

    #endregion

    private void RotateAirborne(Vector2 pathDirection)
    {
        if (pathDirection.sqrMagnitude <= 0.0001f)
            return;

        pathDirection.Normalize();

        float targetZ = Normalize360(Vector2.SignedAngle(Vector2.up, pathDirection));

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = Mathf.MoveTowardsAngle(
            eulerAngles.z,
            targetZ,
            AIRBORNE_ROTATE_SPEED * Time.deltaTime
        );

        enemy.transform.eulerAngles = eulerAngles;
    }

    #endregion
    #endregion

    #region Helpers
    #region Surface
    private bool HasLeftCurrentSurface()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemy.GetWhatIsGround();
        filter.useTriggers = false;

        ContactPoint2D[] contacts = new ContactPoint2D[16];
        int count = rb.GetContacts(filter, contacts);

        if (count <= 0)
            return true;

        Vector2 currentNormal = SurfaceNormal(currentSurface);

        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = contacts[i];

            if (contact.collider == null)
                continue;

            Vector2 normal = contact.normal;
            if (normal.sqrMagnitude <= 0.0001f)
                continue;

            normal.Normalize();

            float sameSurfaceDot = Vector2.Dot(normal, currentNormal);

            if (sameSurfaceDot > 0.9f)
                return false;
        }

        return true;
    }
    private BounceSurface ClassifySurface(Vector2 normal)
    {
        Vector2 normalizedNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;

        if (normalizedNormal.y > 0.7071f)
            return BounceSurface.Floor;

        if (normalizedNormal.y < -0.7071f)
            return BounceSurface.Ceiling;

        return (normalizedNormal.x < 0f) ? BounceSurface.RightWall : BounceSurface.LeftWall;
    }
    private Vector2 SurfaceNormal(BounceSurface surface)
    {
        switch (surface)
        {
            case BounceSurface.Floor:
                return Vector2.up;

            case BounceSurface.Ceiling:
                return Vector2.down;

            case BounceSurface.RightWall:
                return Vector2.left;

            case BounceSurface.LeftWall:
                return Vector2.right;
        }

        return Vector2.up;
    }
    private void AlignToSurfaceNormal(Vector2 normal)
    {
        currentSurfaceNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;
    }
    private float GetSurfaceTargetZ(BounceSurface surface)
    {
        Vector2 surfaceNormal = SurfaceNormal(surface);

        Vector2 feetTarget = -surfaceNormal;
        return Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));
    }
    private void RotateFeetToSurface()
    {
        float targetZ = GetSurfaceTargetZ(currentSurface);

        var eulerAngles = enemy.transform.eulerAngles;

        float smoothTime = Mathf.Max(0.0001f, SURFACE_ALIGN_TIME);
        eulerAngles.z = Mathf.SmoothDampAngle(
            eulerAngles.z,
            targetZ,
            ref surfaceRotateVelocity,
            smoothTime
        );

        enemy.transform.eulerAngles = eulerAngles;

        float remaining = Mathf.Abs(Mathf.DeltaAngle(eulerAngles.z, targetZ));
        if (remaining <= SURFACE_ALIGN_EPS_DEG)
        {
            eulerAngles.z = targetZ;
            enemy.transform.eulerAngles = eulerAngles;
            surfaceRotateVelocity = 0f;
        }
    }
    private bool IsFeetAlignedToSurface()
    {
        float targetZ = GetSurfaceTargetZ(currentSurface);
        float currentZ = enemy.transform.eulerAngles.z;

        return Mathf.Abs(Mathf.DeltaAngle(currentZ, targetZ)) <= SURFACE_ALIGN_EPS_DEG;
    }
    protected virtual BounceSurface ResolveSurfaceFromProbeScan(
    BounceSurface rawSurface,
    BounceMovementHandler<TEnemy>.LandingProbeScan scan
)
    {
        return movementHandler.ResolveSurfaceFromProbeScanDefault(rawSurface, scan);
    }
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

    private float Normalize360(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    protected virtual void PlayAll(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;

        var t = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(t);
    }

    #endregion
}