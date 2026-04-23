using System.Collections.Generic;
using UnityEngine;

public class BounceStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private static readonly List<StateSound> Empty = new();

    protected readonly List<StateSound> enterSounds;
    protected readonly List<StateSound> exitSounds;

    #region Bounce runtime

    #region Enum
    protected enum BounceSurface
    {
        Floor,
        RightWall,
        Ceiling,
        LeftWall
    }

    protected enum BouncePhase
    {
        Attach,
        Launch,
        Airborne,
        Complete
    }

    protected enum BounceJumpType
    {
        Normal,
        FloorToCeiling
    }

    protected enum BounceJumpSide
    {
        Left,
        Right
    }

    protected enum BounceAirMoveType
    {
        None,
        Arc,
        Curve
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

    #region Bounce phases

    protected virtual void AttachPhase()
    {
        SurfacePhase();

        bool isAligned = IsFeetAlignedToSurface();
        if (!isAligned)
            return;

        hasLandingHit = false;
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

        if (TryFindLandingSurface(out Vector2 hitPoint, out Vector2 hitNormal))
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
        plannedJumpType = GetWeightedJumpType();
        plannedJumpSide = GetJumpSide();
        plannedJumpDistance = GetJumpDistance(plannedJumpSide);
        plannedJumpHeight = GetJumpHeight(plannedJumpType);

        launchSurface = currentSurface;
        plannedJumpDirection = GetLocalJumpDirection(plannedJumpSide).normalized;
        plannedAirMoveType = GetAirMoveType(launchSurface, plannedJumpType);

        airStartPosition = enemy.transform.position;
        airTravelDistance = 0f;
        airMoveInitialized = false;
        arcReachedEnd = false;

        Debug.Log($"{enemy.name} PlanJump | Surface={launchSurface} | JumpType={plannedJumpType} | Side={plannedJumpSide} | AirMoveType={plannedAirMoveType} | Distance={plannedJumpDistance} | Height={plannedJumpHeight} | Direction={plannedJumpDirection}");
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

    protected virtual Vector2 GetLocalJumpDirection(BounceJumpSide jumpSide)
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
    protected virtual bool IsJumpSideAtPatrolEdge(BounceJumpSide jumpSide)
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
    protected virtual bool IsJumpSideBlockedByWall(BounceJumpSide jumpSide)
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
    protected virtual Vector2 GetJumpSideProbeOrigin(Collider2D col, Vector2 direction)
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
    protected virtual float GetJumpSideProbeDistance(Collider2D col)
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

        //int normalWeight = 75;
        int normalWeight = 0;
        //int floorToCeilingWeight = 25;
        int floorToCeilingWeight = 100;

        int totalWeight = normalWeight + floorToCeilingWeight;
        int roll = UnityEngine.Random.Range(0, totalWeight);

        if (roll < normalWeight)
            return BounceJumpType.Normal;

        return BounceJumpType.FloorToCeiling;
    }

    protected virtual BounceAirMoveType GetAirMoveType(BounceSurface surface, BounceJumpType jumpType)
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

        if (plannedJumpType == BounceJumpType.FloorToCeiling)
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
            maxHeight = minHeight;
    }

    protected virtual float GetPlayerHeight()
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

        if (ShouldUseSurfaceHeightPadding(patrolSpan, playerHeight))
            patrolSpan -= Mathf.Max(0f, enemy.bounceJumpHeightPadding);

        return Mathf.Max(playerHeight, patrolSpan);
    }

    protected virtual bool ShouldUseSurfaceHeightPadding(float patrolSpan, float playerHeight)
    {
        return patrolSpan > playerHeight * 3f;
    }

    protected virtual float GetFloorToCeilingSpan()
    {
        return Mathf.Max(0.1f, enemy.patrolAreaSize.y);
    }


    #endregion


    #endregion

    #endregion

    #region Move
    protected virtual float MoveSpeed()
    {
        return enemy.moveSpeed * enemy.moveSpeedMultiplier;
    }

    protected virtual void ApplyBounceMovement(Vector2 targetPosition, Vector2 pathDirection)
    {
        rb.MovePosition(targetPosition);
        rb.linearVelocity = pathDirection.normalized * MoveSpeed();
    }

    protected virtual void MoveAirborne()
    {
        switch (plannedAirMoveType)
        {
            case BounceAirMoveType.Arc:
                MoveAirborneArc();
                break;

            case BounceAirMoveType.Curve:
                MoveAirborneCurve();
                break;
        }
    }

    protected virtual void MoveAirborneArc()
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

        Vector2 alongAxis = plannedJumpDirection.normalized;
        Vector2 awayAxis = SurfaceNormal(launchSurface).normalized;

        MoveAirborneArcTravel(moveSpeed, deltaTime, alongAxis, awayAxis);
    }

    protected virtual void MoveAirborneCurve()
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

    #region Helpers
    #region Arc
    protected virtual void MoveAirborneArcTravel(float moveSpeed, float deltaTime, Vector2 alongAxis, Vector2 awayAxis)
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
            RotateAirborne(progress);

            if (progress >= 1f)
                arcReachedEnd = true;

            return;
        }

        rb.linearVelocity = rb.linearVelocity;
    }

    protected virtual float GetArcHeightSlope(float travelDistance, float totalDistance, float maxHeight)
    {
        float progress = Mathf.Clamp01(travelDistance / Mathf.Max(0.001f, totalDistance));
        return (Mathf.PI * maxHeight / Mathf.Max(0.001f, totalDistance)) * Mathf.Cos(progress * Mathf.PI);
    }

    protected virtual float GetArcRotationSign()
    {
        return plannedJumpSide == BounceJumpSide.Left ? -1f : 1f;
    }
    #endregion

    #region Curve
    protected virtual void MoveAirborneCurveFromFloor()
    {
        Debug.Log($"{enemy.name} MoveAirborneCurveFromFloor");

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

            MoveAirborneCurveTravel(progress, targetPosition, pathDirection);

            if (progress >= 1f)
                curveReachedEnd = true;

            return;
        }

        Vector2 upDirection = Vector2.up;
        Vector2 continuePosition = rb.position + upDirection * (MoveSpeed() * deltaTime);

        MoveAirborneCurveTravel(1f, continuePosition, upDirection);
    }

    protected virtual void MoveAirborneCurveFromWall()
    {
        Debug.Log($"{enemy.name} MoveAirborneCurveFromWall");
    }

    protected virtual void MoveAirborneCurveFromCeiling()
    {
        Debug.Log($"{enemy.name} MoveAirborneCurveFromCeiling");

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

            MoveAirborneCurveTravel(progress, targetPosition, pathDirection);

            if (progress >= 1f)
                curveReachedEnd = true;

            return;
        }

        float fallX = plannedJumpDirection.x;
        if (Mathf.Abs(fallX) <= 0.0001f)
            fallX = plannedJumpSide == BounceJumpSide.Right ? 1f : -1f;

        Vector2 fallDirection = new Vector2(fallX * 0.35f, -1f).normalized;
        Vector2 continuePosition = rb.position + (fallDirection * (MoveSpeed() * deltaTime));

        MoveAirborneCurveTravel(1f, continuePosition, fallDirection);
    }

    #region Helpers
    protected virtual void MoveAirborneCurveTravel(float curveProgress, Vector2 targetPosition, Vector2 pathDirection)
    {
        ApplyBounceMovement(targetPosition, pathDirection);
        RotateAirborne(curveProgress);
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

    protected virtual void RotateAirborne(float arcProgress)
    {
        float startZ = GetSurfaceTargetZ(launchSurface);
        float arcRotation = arcProgress * 180f * -GetArcRotationSign();
        float targetZ = Normalize360(startZ + arcRotation);

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
    protected virtual bool HasLeftCurrentSurface()
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
    protected virtual BounceSurface ClassifySurface(Vector2 normal)
    {
        Vector2 normalizedNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;

        if (normalizedNormal.y > 0.7071f)
            return BounceSurface.Floor;

        if (normalizedNormal.y < -0.7071f)
            return BounceSurface.Ceiling;

        return (normalizedNormal.x < 0f) ? BounceSurface.RightWall : BounceSurface.LeftWall;
    }
    protected virtual Vector2 SurfaceNormal(BounceSurface surface)
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
    protected virtual void AlignToSurfaceNormal(Vector2 normal)
    {
        currentSurfaceNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;
    }
    protected virtual float GetSurfaceTargetZ(BounceSurface surface)
    {
        Vector2 surfaceNormal = SurfaceNormal(surface);

        Vector2 feetTarget = -surfaceNormal;
        return Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));
    }
    protected virtual void RotateFeetToSurface()
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
    protected virtual bool IsFeetAlignedToSurface()
    {
        float targetZ = GetSurfaceTargetZ(currentSurface);
        float currentZ = enemy.transform.eulerAngles.z;

        return Mathf.Abs(Mathf.DeltaAngle(currentZ, targetZ)) <= SURFACE_ALIGN_EPS_DEG;
    }
    #endregion

    #region Airborne
    protected virtual bool TryFindLandingSurface(out Vector2 hitPoint, out Vector2 hitNormal)
    {
        hitPoint = default;
        hitNormal = default;

        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemy.GetWhatIsGround();
        filter.useTriggers = false;

        Collider2D[] hitColliders = new Collider2D[16];
        int count = enemyCollider.GetContacts(filter, hitColliders);

        if (count <= 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            Collider2D hitCollider = hitColliders[i];

            if (hitCollider == null)
                continue;

            ColliderDistance2D distance = enemyCollider.Distance(hitCollider);

            Vector2 normal = -distance.normal;
            if (normal.sqrMagnitude <= 0.0001f)
                continue;

            hitPoint = distance.pointA;
            hitNormal = normal.normalized;
            return true;
        }

        return false;
    }

    #endregion

    #region Gravity

    protected virtual void CacheDefaultGravity()
    {
        if (gravityCached)
            return;

        savedDefaultGravity = rb.gravityScale;
        gravityCached = true;
    }

    protected virtual void SetAttachedGravity()
    {
        rb.gravityScale = 0f;
    }

    protected virtual void SetAirborneGravity()
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
    protected virtual float Normalize360(float angle)
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