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
        MoveEnemy();

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
        return jumpSide == BounceJumpSide.Left
            ? -(Vector2)enemy.transform.right
            : (Vector2)enemy.transform.right;
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

        int normalWeight = 75;
        int floorToCeilingWeight = 25;

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
            case BounceSurface.Ceiling:
                return BounceAirMoveType.Curve;

            case BounceSurface.Floor:
            case BounceSurface.RightWall:
            case BounceSurface.LeftWall:
            default:
                return BounceAirMoveType.Arc;
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
                return GetWallJumpMaxHeight(playerHeight);

            case BounceSurface.Ceiling:
                return GetCeilingJumpMaxHeight(playerHeight);
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

    protected virtual float GetWallJumpMaxHeight(float playerHeight)
    {
        float patrolSpan = GetWallToWallSpan();

        if (ShouldUseSurfaceHeightPadding(patrolSpan, playerHeight))
            patrolSpan -= Mathf.Max(0f, enemy.bounceJumpHeightPadding);

        return Mathf.Max(playerHeight, patrolSpan);
    }

    protected virtual float GetCeilingJumpMaxHeight(float playerHeight)
    {
        float patrolSpan = GetFloorToCeilingSpan();
        float curveAmount = patrolSpan * 0.5f;

        return Mathf.Max(playerHeight, curveAmount);
    }

    protected virtual bool ShouldUseSurfaceHeightPadding(float patrolSpan, float playerHeight)
    {
        return patrolSpan > playerHeight * 3f;
    }

    protected virtual float GetFloorToCeilingSpan()
    {
        return Mathf.Max(0.1f, enemy.patrolAreaSize.y);
    }

    protected virtual float GetWallToWallSpan()
    {
        return Mathf.Max(0.1f, enemy.patrolAreaSize.x);
    }

    #endregion


    #endregion

    #endregion

    #region Move
    protected virtual void MoveEnemy()
    {
        Vector2 launchDirection = currentSurfaceNormal.normalized;
        float moveSpeed = enemy.moveSpeed * enemy.moveSpeedMultiplier;

        rb.linearVelocity = launchDirection * moveSpeed;
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

            default:
                RotateInMovementDirection(rb.linearVelocity);
                break;
        }
    }

    protected virtual void MoveAirborneArc()
    {
        float moveSpeed = enemy.moveSpeed * enemy.moveSpeedMultiplier;
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

        if (!arcReachedEnd)
        {
            MoveAirborneArcTravel(moveSpeed, deltaTime, alongAxis, awayAxis);
            return;
        }

        MoveAirborneArcLand(moveSpeed, deltaTime, awayAxis);
    }
    protected virtual void MoveAirborneCurve()
    {
        float moveSpeed = enemy.moveSpeed * enemy.moveSpeedMultiplier;
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

        Vector2 startPoint = airStartPosition;
        Vector2 endPoint = GetCurveEndPoint(startPoint);
        BounceSurface targetSurface = GetCurveTargetSurface();

        Vector2 startControl = startPoint + SurfaceNormal(launchSurface).normalized * plannedJumpHeight;
        Vector2 endControl = endPoint - SurfaceNormal(targetSurface).normalized * plannedJumpHeight;

        float curveLength = Mathf.Max(0.001f, GetCurveApproxLength(startPoint, startControl, endControl, endPoint));

        float progress = Mathf.Clamp01(airTravelDistance / curveLength);
        Vector2 tangent = GetCurveTangent(startPoint, startControl, endControl, endPoint, progress);
        float tangentLength = Mathf.Max(0.001f, tangent.magnitude);

        float progressStep = (moveSpeed * deltaTime) / tangentLength;
        float nextProgress = Mathf.Clamp01(progress + progressStep);

        Vector2 targetPosition = GetCurvePoint(startPoint, startControl, endControl, endPoint, nextProgress);
        Vector2 pathDirection = GetCurveTangent(startPoint, startControl, endControl, endPoint, nextProgress).normalized;

        airTravelDistance = nextProgress * curveLength;

        rb.MovePosition(targetPosition);
        rb.linearVelocity = pathDirection * moveSpeed;
        RotateInMovementDirection(pathDirection);
    }

    #region Helpers
    #region Arc
    protected virtual void MoveAirborneArcTravel(float moveSpeed, float deltaTime, Vector2 alongAxis, Vector2 awayAxis)
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

        rb.MovePosition(targetPosition);
        rb.linearVelocity = pathDirection * moveSpeed;
        RotateInMovementDirection(pathDirection, progress);

        if (progress >= 1f)
            arcReachedEnd = true;
    }

    protected virtual void MoveAirborneArcLand(float moveSpeed, float deltaTime, Vector2 awayAxis)
    {
        Vector2 returnDirection = (-awayAxis).normalized;
        Vector2 targetPosition = rb.position + returnDirection * (moveSpeed * deltaTime);

        rb.MovePosition(targetPosition);
        rb.linearVelocity = returnDirection * moveSpeed;
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
    protected virtual Vector2 GetCurveEndPoint(Vector2 startPoint)
    {
        return startPoint + plannedJumpDirection.normalized * plannedJumpDistance;
    }

    protected virtual BounceSurface GetCurveTargetSurface()
    {
        if (plannedJumpType == BounceJumpType.FloorToCeiling)
            return BounceSurface.Ceiling;

        if (launchSurface == BounceSurface.Ceiling)
            return BounceSurface.Floor;

        return launchSurface;
    }

    protected virtual Vector2 GetCurvePoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;

        return (u * u * u) * p0
             + (3f * u * u * t) * p1
             + (3f * u * t * t) * p2
             + (t * t * t) * p3;
    }

    protected virtual Vector2 GetCurveTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;

        return (3f * u * u) * (p1 - p0)
             + (6f * u * t) * (p2 - p1)
             + (3f * t * t) * (p3 - p2);
    }

    protected virtual float GetCurveApproxLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps = 12)
    {
        float length = 0f;
        Vector2 previous = p0;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = GetCurvePoint(p0, p1, p2, p3, t);
            length += Vector2.Distance(previous, point);
            previous = point;
        }

        return length;
    }
    #endregion
    protected virtual void RotateInMovementDirection(Vector2 moveDirection, float arcProgress = -1f)
    {
        float targetZ;

        if (plannedAirMoveType == BounceAirMoveType.Arc && arcProgress >= 0f && !arcReachedEnd)
        {
            float startZ = GetSurfaceTargetZ(launchSurface);
            float arcRotation = arcProgress * 360f * GetArcRotationSign();
            targetZ = Normalize360(startZ + arcRotation);
        }
        else
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
                return;

            targetZ = Normalize360(Vector2.SignedAngle(Vector2.down, -moveDirection.normalized));
        }

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