using System.Collections.Generic;
using UnityEngine;

public class BounceMovementHandler<TEnemy> where TEnemy : Enemy_Regular
{
    private readonly BounceStateBase<TEnemy> state;
    private readonly TEnemy enemy;

    public BounceMovementHandler(BounceStateBase<TEnemy> state, TEnemy enemy)
    {
        this.state = state;
        this.enemy = enemy;
    }

    #region Enums

    public enum BounceEdgePoint
    {
        None,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }

    #endregion

    #region Secondary surface runtime

    private readonly List<BounceStateBase<TEnemy>.BounceSurface> ignoredLaunchSurfaces = new();
    private readonly List<BounceStateBase<TEnemy>.BounceSurface> ignoredLaunchEdgeSurfaces = new();

    private Vector2 landingIgnoreFailSafeLastPosition;
    private Vector2 landingIgnoreFailSafePathDirection = Vector2.right;
    private float landingIgnoreFailSafeAirTime;
    private float landingIgnoreBlockedTimer;
    private float landingIgnoreGroundContactTimer;

    private const float LANDING_IGNORE_EDGE_BLOCKED_TIME = 0.28f;
    private const float LANDING_IGNORE_GROUND_CONTACT_TIME = 0.06f;
    private const float LANDING_IGNORE_FAIL_SAFE_GRACE = 0.08f;
    private const float LANDING_IGNORE_BLOCKED_TIME = 0.12f;
    private const float LANDING_IGNORE_FORWARD_PROGRESS_FACTOR = 0.15f;

    internal Vector2 LandingIgnoreFailSafePathDirection
    {
        get => landingIgnoreFailSafePathDirection;
        set => landingIgnoreFailSafePathDirection = value;
    }

    #endregion


    #region Movement runtime

    private Vector2 airStartPosition;
    private float airTravelDistance;
    private bool airMoveInitialized;
    private bool arcReachedEnd;

    private float curveTravelDistance;
    private bool curveReachedEnd;

    private bool edgeBouncePlanBuilt;
    private Vector2 edgeBounceHitDirection;
    private Vector2 edgeBounceSurfaceNormal;

    private bool edgeSurfaceReleaseActive;
    private bool edgeSurfaceReleased;

    private BounceEdgePoint edgeBouncePoint = BounceEdgePoint.None;
    private Vector2 edgeBounceProbeHitPoint;

    private float surfaceRotateVelocity;

    private const float AIRBORNE_ROTATE_SPEED = 720f;
    private const float SURFACE_ALIGN_TIME = 0.06f;
    private const float SURFACE_ALIGN_EPS_DEG = 0.75f;


    public void ResetMovementRuntimeForNewPlan(Vector2 startPosition)
    {
        airStartPosition = startPosition;
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
    }

    #endregion

    #region Surface Hit

    #region Landing surface

    public bool TryFindLandingSurface(out Vector2 hitPoint, out Vector2 hitNormal)
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

            Vector2 rawNormal = -distance.normal;
            if (rawNormal.sqrMagnitude <= 0.0001f)
                continue;

            BounceStateBase<TEnemy>.BounceSurface rawSurface = ClassifySurface(rawNormal);

            if (ResolveLandingSurfaceWithProbes(
                rawSurface,
                rawNormal,
                distance.pointA,
                out hitPoint,
                out hitNormal
            ))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Landing probe data

    public struct LandingProbeScan
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;

        public bool upLeft;
        public bool upRight;
        public bool downLeft;
        public bool downRight;

        public RaycastHit2D upHit;
        public RaycastHit2D downHit;
        public RaycastHit2D leftHit;
        public RaycastHit2D rightHit;

        public RaycastHit2D upLeftHit;
        public RaycastHit2D upRightHit;
        public RaycastHit2D downLeftHit;
        public RaycastHit2D downRightHit;
    }

    #endregion

    #region Landing probe resolve

    private bool ResolveLandingSurfaceWithProbes(
        BounceStateBase<TEnemy>.BounceSurface rawSurface,
        Vector2 rawNormal,
        Vector2 rawPoint,
        out Vector2 resolvedPoint,
        out Vector2 resolvedNormal
    )
    {
        resolvedPoint = rawPoint;
        resolvedNormal = SurfaceNormal(rawSurface);

        if (!ScanLandingProbes(out LandingProbeScan scan))
            return false;

        if (!HasMainLandingProbeHit(scan))
        {
            if (HasDiagonalLandingProbeHit(scan))
            {
                BounceEdgePoint previousEdgePoint = edgeBouncePoint;
                BounceEdgePoint detectedEdgePoint = GetEdgeBouncePointFromProbeScan(scan);

                edgeBouncePoint = detectedEdgePoint;
                edgeBounceProbeHitPoint = GetEdgeBounceProbeHitPointFromProbeScan(scan);

                bool forceRebuild =
                    state.MovementPlannedAirMoveType != BounceStateBase<TEnemy>.BounceAirMoveType.Edge ||
                    !edgeBouncePlanBuilt ||
                    previousEdgePoint != detectedEdgePoint;

                RequestEdgeBouncePlan(scan, rawNormal, forceRebuild);
            }

            return false;
        }

        BounceStateBase<TEnemy>.BounceSurface resolvedSurface = ResolveSurfaceFromProbeScanDefault(rawSurface, scan);

        if (IsLandingSurfaceIgnored(resolvedSurface))
        {
            return false;
        }

        if (TryGetProbePointForSurface(scan, resolvedSurface, out Vector2 probePoint))
            resolvedPoint = probePoint;

        resolvedNormal = SurfaceNormal(resolvedSurface);
        return true;
    }

    private bool HasMainLandingProbeHit(LandingProbeScan scan)
    {
        return scan.up || scan.down || scan.left || scan.right;
    }

    private bool HasDiagonalLandingProbeHit(LandingProbeScan scan)
    {
        return scan.upLeft || scan.upRight || scan.downLeft || scan.downRight;
    }

    private Vector2 GetEdgeBounceProbeHitPointFromProbeScan(LandingProbeScan scan)
    {
        switch (edgeBouncePoint)
        {
            case BounceEdgePoint.UpLeft:
                return scan.downRightHit.point;

            case BounceEdgePoint.UpRight:
                return scan.downLeftHit.point;

            case BounceEdgePoint.DownLeft:
                return scan.upRightHit.point;

            case BounceEdgePoint.DownRight:
                return scan.upLeftHit.point;
        }

        return Vector2.zero;
    }

    private BounceEdgePoint GetEdgeBouncePointFromProbeScan(LandingProbeScan scan)
    {
        if (scan.upLeft)
            return BounceEdgePoint.DownRight;

        if (scan.upRight)
            return BounceEdgePoint.DownLeft;

        if (scan.downLeft)
            return BounceEdgePoint.UpRight;

        if (scan.downRight)
            return BounceEdgePoint.UpLeft;

        return BounceEdgePoint.None;
    }

    #endregion

    #region Landing probe scan

    private bool ScanLandingProbes(out LandingProbeScan scan)
    {
        scan = default;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null)
            return false;

        Bounds bounds = col.bounds;
        Vector2 origin = bounds.center;

        scan.up = CastLandingProbe(origin, bounds, Vector2.up, out scan.upHit);
        scan.down = CastLandingProbe(origin, bounds, Vector2.down, out scan.downHit);
        scan.left = CastLandingProbe(origin, bounds, Vector2.left, out scan.leftHit);
        scan.right = CastLandingProbe(origin, bounds, Vector2.right, out scan.rightHit);

        scan.upLeft = CastLandingProbe(origin, bounds, (Vector2.up + Vector2.left).normalized, out scan.upLeftHit);
        scan.upRight = CastLandingProbe(origin, bounds, (Vector2.up + Vector2.right).normalized, out scan.upRightHit);
        scan.downLeft = CastLandingProbe(origin, bounds, (Vector2.down + Vector2.left).normalized, out scan.downLeftHit);
        scan.downRight = CastLandingProbe(origin, bounds, (Vector2.down + Vector2.right).normalized, out scan.downRightHit);

        return scan.up || scan.down || scan.left || scan.right ||
               scan.upLeft || scan.upRight || scan.downLeft || scan.downRight;
    }

    private bool CastLandingProbe(
        Vector2 origin,
        Bounds bounds,
        Vector2 direction,
        out RaycastHit2D hit
    )
    {
        hit = default;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();

        float distance = GetLandingProbeDistance(bounds, direction);

        hit = Physics2D.Raycast(
            origin,
            direction,
            distance,
            enemy.GetWhatIsGround()
        );

        return hit.collider != null;
    }

    private float GetLandingProbeDistance(Bounds bounds, Vector2 direction)
    {
        Vector2 absDirection = new Vector2(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
        float bodyReach = Vector2.Dot(bounds.extents, absDirection);
        float extraReach = Mathf.Max(0.05f, Physics2D.defaultContactOffset * 4f);

        return Mathf.Max(0.05f, bodyReach + extraReach);
    }

    #endregion

    #region Landing surface selection

    internal BounceStateBase<TEnemy>.BounceSurface ResolveSurfaceFromProbeScanDefault(
        BounceStateBase<TEnemy>.BounceSurface rawSurface,
        LandingProbeScan scan
    )
    {
        if (TryResolveFlatProbeSurface(scan, out BounceStateBase<TEnemy>.BounceSurface flatSurface))
            return flatSurface;

        return ResolveSurfaceByProbeScore(rawSurface, scan);
    }

    private bool TryResolveFlatProbeSurface(
        LandingProbeScan scan,
        out BounceStateBase<TEnemy>.BounceSurface surface
    )
    {
        surface = BounceStateBase<TEnemy>.BounceSurface.Floor;

        bool floorFlat = scan.down && scan.downLeft && scan.downRight;
        bool ceilingFlat = scan.up && scan.upLeft && scan.upRight;
        bool leftWallFlat = scan.left && scan.upLeft && scan.downLeft;
        bool rightWallFlat = scan.right && scan.upRight && scan.downRight;

        int flatCount = 0;

        if (floorFlat)
        {
            surface = BounceStateBase<TEnemy>.BounceSurface.Floor;
            flatCount++;
        }

        if (ceilingFlat)
        {
            surface = BounceStateBase<TEnemy>.BounceSurface.Ceiling;
            flatCount++;
        }

        if (leftWallFlat)
        {
            surface = BounceStateBase<TEnemy>.BounceSurface.LeftWall;
            flatCount++;
        }

        if (rightWallFlat)
        {
            surface = BounceStateBase<TEnemy>.BounceSurface.RightWall;
            flatCount++;
        }

        return flatCount == 1;
    }

    private BounceStateBase<TEnemy>.BounceSurface ResolveSurfaceByProbeScore(
        BounceStateBase<TEnemy>.BounceSurface rawSurface,
        LandingProbeScan scan
    )
    {
        int floorScore = GetProbeScore(BounceStateBase<TEnemy>.BounceSurface.Floor, scan);
        int ceilingScore = GetProbeScore(BounceStateBase<TEnemy>.BounceSurface.Ceiling, scan);
        int leftWallScore = GetProbeScore(BounceStateBase<TEnemy>.BounceSurface.LeftWall, scan);
        int rightWallScore = GetProbeScore(BounceStateBase<TEnemy>.BounceSurface.RightWall, scan);

        int bestScore = Mathf.Max(
            Mathf.Max(floorScore, ceilingScore),
            Mathf.Max(leftWallScore, rightWallScore)
        );

        if (bestScore <= 0)
            return rawSurface;

        if (GetProbeScore(rawSurface, scan) == bestScore)
            return rawSurface;

        BounceStateBase<TEnemy>.BounceSurface movementSurface = GetSurfaceFromAirVelocity();
        if (GetProbeScore(movementSurface, scan) == bestScore)
            return movementSurface;

        if (floorScore == bestScore)
            return BounceStateBase<TEnemy>.BounceSurface.Floor;

        if (ceilingScore == bestScore)
            return BounceStateBase<TEnemy>.BounceSurface.Ceiling;

        if (leftWallScore == bestScore)
            return BounceStateBase<TEnemy>.BounceSurface.LeftWall;

        return BounceStateBase<TEnemy>.BounceSurface.RightWall;
    }

    private int GetProbeScore(
        BounceStateBase<TEnemy>.BounceSurface surface,
        LandingProbeScan scan
    )
    {
        switch (surface)
        {
            case BounceStateBase<TEnemy>.BounceSurface.Floor:
                return (scan.down ? 4 : 0) +
                       (scan.downLeft ? 1 : 0) +
                       (scan.downRight ? 1 : 0);

            case BounceStateBase<TEnemy>.BounceSurface.Ceiling:
                return (scan.up ? 4 : 0) +
                       (scan.upLeft ? 1 : 0) +
                       (scan.upRight ? 1 : 0);

            case BounceStateBase<TEnemy>.BounceSurface.LeftWall:
                return (scan.left ? 4 : 0) +
                       (scan.upLeft ? 1 : 0) +
                       (scan.downLeft ? 1 : 0);

            case BounceStateBase<TEnemy>.BounceSurface.RightWall:
                return (scan.right ? 4 : 0) +
                       (scan.upRight ? 1 : 0) +
                       (scan.downRight ? 1 : 0);
        }

        return 0;
    }

    private BounceStateBase<TEnemy>.BounceSurface GetSurfaceFromAirVelocity()
    {
        Vector2 velocity = state.MovementRigidbody.linearVelocity;

        if (velocity.sqrMagnitude <= 0.0001f)
            return state.MovementCurrentSurface;

        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
        {
            return velocity.x < 0f
                ? BounceStateBase<TEnemy>.BounceSurface.LeftWall
                : BounceStateBase<TEnemy>.BounceSurface.RightWall;
        }

        return velocity.y > 0f
            ? BounceStateBase<TEnemy>.BounceSurface.Ceiling
            : BounceStateBase<TEnemy>.BounceSurface.Floor;
    }

    private bool TryGetProbePointForSurface(
        LandingProbeScan scan,
        BounceStateBase<TEnemy>.BounceSurface surface,
        out Vector2 point
    )
    {
        point = default;

        switch (surface)
        {
            case BounceStateBase<TEnemy>.BounceSurface.Floor:
                if (scan.down) { point = scan.downHit.point; return true; }
                if (scan.downLeft) { point = scan.downLeftHit.point; return true; }
                if (scan.downRight) { point = scan.downRightHit.point; return true; }
                break;

            case BounceStateBase<TEnemy>.BounceSurface.Ceiling:
                if (scan.up) { point = scan.upHit.point; return true; }
                if (scan.upLeft) { point = scan.upLeftHit.point; return true; }
                if (scan.upRight) { point = scan.upRightHit.point; return true; }
                break;

            case BounceStateBase<TEnemy>.BounceSurface.LeftWall:
                if (scan.left) { point = scan.leftHit.point; return true; }
                if (scan.upLeft) { point = scan.upLeftHit.point; return true; }
                if (scan.downLeft) { point = scan.downLeftHit.point; return true; }
                break;

            case BounceStateBase<TEnemy>.BounceSurface.RightWall:
                if (scan.right) { point = scan.rightHit.point; return true; }
                if (scan.upRight) { point = scan.upRightHit.point; return true; }
                if (scan.downRight) { point = scan.downRightHit.point; return true; }
                break;
        }

        return false;
    }

    #endregion

    #endregion

    #region Surface Ignored

    #region Secondary surface ignore

    public void CacheIgnoredSecondarySurface()
    {
        ignoredLaunchSurfaces.Clear();
        ignoredLaunchEdgeSurfaces.Clear();

        ProbeIgnoredLaunchSurface(BounceStateBase<TEnemy>.BounceJumpSide.Left);
        ProbeIgnoredLaunchSurface(BounceStateBase<TEnemy>.BounceJumpSide.Right);
        ProbeIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceJumpSide.Left);
        ProbeIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceJumpSide.Right);

        ResetLandingIgnoreFailSafeTracking();
    }

    public void UpdateIgnoredSecondarySurface()
    {
        for (int i = ignoredLaunchSurfaces.Count - 1; i >= 0; i--)
        {
            BounceStateBase<TEnemy>.BounceSurface surface = ignoredLaunchSurfaces[i];

            bool stillOnLeft = IsTouchingSurface(BounceStateBase<TEnemy>.BounceJumpSide.Left, surface);
            bool stillOnRight = IsTouchingSurface(BounceStateBase<TEnemy>.BounceJumpSide.Right, surface);

            if (!stillOnLeft && !stillOnRight)
                ignoredLaunchSurfaces.RemoveAt(i);
        }

        for (int i = ignoredLaunchEdgeSurfaces.Count - 1; i >= 0; i--)
        {
            BounceStateBase<TEnemy>.BounceSurface surface = ignoredLaunchEdgeSurfaces[i];

            bool stillOnLeft = IsTouchingSurface(BounceStateBase<TEnemy>.BounceJumpSide.Left, surface);
            bool stillOnRight = IsTouchingSurface(BounceStateBase<TEnemy>.BounceJumpSide.Right, surface);

            bool stillOnLeftEdge = IsLaunchEdgeProbeHit(BounceStateBase<TEnemy>.BounceJumpSide.Left, surface);
            bool stillOnRightEdge = IsLaunchEdgeProbeHit(BounceStateBase<TEnemy>.BounceJumpSide.Right, surface);

            if (!stillOnLeft && !stillOnRight && !stillOnLeftEdge && !stillOnRightEdge)
                ignoredLaunchEdgeSurfaces.RemoveAt(i);
        }

        UpdateLandingIgnoreFailSafe();
    }

    public void ClearIgnoredLaunchSurfaces()
    {
        ignoredLaunchSurfaces.Clear();
    }

    private void CacheIgnoredSurfacesForEdgeRelease()
    {
        ignoredLaunchSurfaces.Clear();
        ignoredLaunchEdgeSurfaces.Clear();

        CacheIgnoredSurfacesFromEdgePoint();

        ResetLandingIgnoreFailSafeTracking();
    }

    private bool IsTouchingSurface(
        BounceStateBase<TEnemy>.BounceJumpSide jumpSide,
        BounceStateBase<TEnemy>.BounceSurface surface
    )
    {
        Vector2 direction = state.MovementGetLocalJumpDirection(jumpSide).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        if (!ProbeSurface(direction, out BounceStateBase<TEnemy>.BounceSurface hitSurface))
            return false;

        return hitSurface == surface;
    }

    private void ResetLandingIgnoreFailSafeTracking()
    {
        landingIgnoreFailSafeLastPosition = state.MovementRigidbody.position;
        landingIgnoreFailSafeAirTime = 0f;
        landingIgnoreBlockedTimer = 0f;
        landingIgnoreGroundContactTimer = 0f;

        if (landingIgnoreFailSafePathDirection.sqrMagnitude <= 0.0001f)
            landingIgnoreFailSafePathDirection = state.MovementCurrentSurfaceNormal.normalized;
    }

    private void UpdateLandingIgnoreFailSafe()
    {
        bool hasIgnoredSurface = ignoredLaunchSurfaces.Count > 0 || ignoredLaunchEdgeSurfaces.Count > 0;

        if (!hasIgnoredSurface)
        {
            ResetLandingIgnoreFailSafeTracking();
            return;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        landingIgnoreFailSafeAirTime += deltaTime;

        Vector2 currentPosition = state.MovementRigidbody.position;
        Vector2 actualMove = currentPosition - landingIgnoreFailSafeLastPosition;
        landingIgnoreFailSafeLastPosition = currentPosition;

        if (landingIgnoreFailSafeAirTime < LANDING_IGNORE_FAIL_SAFE_GRACE)
            return;

        if (!HasGroundContactWhileLandingBlocked())
        {
            landingIgnoreGroundContactTimer = 0f;
            landingIgnoreBlockedTimer = 0f;
            return;
        }

        landingIgnoreGroundContactTimer += deltaTime;

        if (landingIgnoreGroundContactTimer < LANDING_IGNORE_GROUND_CONTACT_TIME)
            return;

        Vector2 expectedDirection = landingIgnoreFailSafePathDirection;

        if (expectedDirection.sqrMagnitude <= 0.0001f)
        {
            ResetLandingIgnoreFailSafeTracking();
            return;
        }

        expectedDirection.Normalize();

        float forwardProgress = Vector2.Dot(actualMove, expectedDirection);
        float expectedMove = MoveSpeed() * deltaTime;
        float minForwardProgress = expectedMove * LANDING_IGNORE_FORWARD_PROGRESS_FACTOR;

        bool forwardProgressStopped = forwardProgress <= minForwardProgress;

        if (forwardProgressStopped)
            landingIgnoreBlockedTimer += deltaTime;
        else
            landingIgnoreBlockedTimer = 0f;

        float blockedTimeLimit = ignoredLaunchEdgeSurfaces.Count > 0
            ? LANDING_IGNORE_EDGE_BLOCKED_TIME
            : LANDING_IGNORE_BLOCKED_TIME;

        if (landingIgnoreBlockedTimer < blockedTimeLimit)
            return;

        ClearLandingIgnoreBlocks();
    }

    private void ClearLandingIgnoreBlocks()
    {
        ignoredLaunchSurfaces.Clear();
        ignoredLaunchEdgeSurfaces.Clear();

        landingIgnoreBlockedTimer = 0f;
        landingIgnoreFailSafeAirTime = 0f;
    }

    #endregion

    #region Secondary surface probe helpers

    private void ProbeIgnoredLaunchSurface(BounceStateBase<TEnemy>.BounceJumpSide jumpSide)
    {
        Vector2 direction = state.MovementGetLocalJumpDirection(jumpSide).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        if (!ProbeSurface(direction, out BounceStateBase<TEnemy>.BounceSurface hitSurface))
            return;

        if (hitSurface == state.MovementCurrentSurface)
            return;

        if (ignoredLaunchSurfaces.Contains(hitSurface))
            return;

        ignoredLaunchSurfaces.Add(hitSurface);
    }

    private void ProbeIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceJumpSide jumpSide)
    {
        if (!ProbeLaunchEdgeSurface(jumpSide, out BounceStateBase<TEnemy>.BounceSurface hitSurface))
            return;

        if (hitSurface == state.MovementCurrentSurface)
            return;

        if (ignoredLaunchEdgeSurfaces.Contains(hitSurface))
            return;

        ignoredLaunchEdgeSurfaces.Add(hitSurface);
    }

    private bool IsLaunchEdgeProbeHit(
        BounceStateBase<TEnemy>.BounceJumpSide jumpSide,
        BounceStateBase<TEnemy>.BounceSurface surface
    )
    {
        if (!ProbeLaunchEdgeSurface(jumpSide, out BounceStateBase<TEnemy>.BounceSurface hitSurface))
            return false;

        return hitSurface == surface;
    }

    private bool ProbeLaunchEdgeSurface(
        BounceStateBase<TEnemy>.BounceJumpSide jumpSide,
        out BounceStateBase<TEnemy>.BounceSurface hitSurface
    )
    {
        hitSurface = BounceStateBase<TEnemy>.BounceSurface.Floor;

        Vector2 sideDirection = state.MovementGetLocalJumpDirection(jumpSide).normalized;
        if (sideDirection.sqrMagnitude <= 0.0001f)
            return false;

        Vector2 awayFromSurface = -state.MovementCurrentSurfaceNormal.normalized;
        Vector2 edgeDirection = (sideDirection + awayFromSurface).normalized;

        if (edgeDirection.sqrMagnitude <= 0.0001f)
            return false;

        return ProbeSurface(edgeDirection, out hitSurface);
    }

    private bool ProbeSurface(
        Vector2 direction,
        out BounceStateBase<TEnemy>.BounceSurface hitSurface
    )
    {
        hitSurface = BounceStateBase<TEnemy>.BounceSurface.Floor;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null)
            return false;

        Vector2 origin = state.MovementGetJumpSideProbeOrigin(col, direction);
        float distance = state.MovementGetJumpSideProbeDistance(col);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, enemy.GetWhatIsGround());
        if (!hit.collider)
            return false;

        hitSurface = ClassifySurface(hit.normal);
        return true;
    }

    private bool HasGroundContactWhileLandingBlocked()
    {
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemy.GetWhatIsGround();
        filter.useTriggers = false;

        Collider2D[] hitColliders = new Collider2D[8];
        int count = enemyCollider.GetContacts(filter, hitColliders);

        return count > 0;
    }

    private bool IsLandingSurfaceIgnored(BounceStateBase<TEnemy>.BounceSurface surface)
    {
        return ignoredLaunchSurfaces.Contains(surface) ||
               ignoredLaunchEdgeSurfaces.Contains(surface);
    }

    private void CacheIgnoredSurfacesFromEdgePoint()
    {
        switch (edgeBouncePoint)
        {
            case BounceEdgePoint.UpLeft:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Floor);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.RightWall);
                break;

            case BounceEdgePoint.UpRight:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Floor);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.LeftWall);
                break;

            case BounceEdgePoint.DownLeft:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Ceiling);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.RightWall);
                break;

            case BounceEdgePoint.DownRight:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Ceiling);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.LeftWall);
                break;
        }
    }

    private void AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface surface)
    {
        if (ignoredLaunchEdgeSurfaces.Contains(surface))
            return;

        ignoredLaunchEdgeSurfaces.Add(surface);
    }

    #endregion

    #endregion

    #region Movement

    #region Main Movement

    public void MoveAirborne()
    {
        switch (state.MovementPlannedAirMoveType)
        {
            case BounceStateBase<TEnemy>.BounceAirMoveType.Arc:
                MoveAirborneArc();
                break;

            case BounceStateBase<TEnemy>.BounceAirMoveType.Curve:
                MoveAirborneCurve();
                break;

            case BounceStateBase<TEnemy>.BounceAirMoveType.Edge:
                MoveAirborneEdge();
                break;
        }
    }

    private void MoveAirborneArc()
    {
        if (!TryBuildMoveFrame(out MoveFrame moveFrame))
            return;

        InitializeAirMoveRuntime(MovementRuntimeType.Arc);

        Vector2 alongAxis = state.MovementPlannedJumpDirection.sqrMagnitude > 0.0001f
            ? state.MovementPlannedJumpDirection.normalized
            : Vector2.right;

        Vector2 awayAxis = state.MovementPlannedArcAwayAxis.sqrMagnitude > 0.0001f
            ? state.MovementPlannedArcAwayAxis.normalized
            : SurfaceNormal(state.MovementLaunchSurface).normalized;

        MoveArcPath(moveFrame, alongAxis, awayAxis);
    }

    private void MoveAirborneCurve()
    {
        switch (state.MovementLaunchSurface)
        {
            case BounceStateBase<TEnemy>.BounceSurface.Floor:
                MoveAirborneCurveFromFloor();
                break;

            case BounceStateBase<TEnemy>.BounceSurface.RightWall:
            case BounceStateBase<TEnemy>.BounceSurface.LeftWall:
                MoveAirborneCurveFromWall();
                break;

            case BounceStateBase<TEnemy>.BounceSurface.Ceiling:
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

    private void MoveAirborneCurveFromFloor()
    {
        InitializeAirMoveRuntime(MovementRuntimeType.Curve);

        if (!TryBuildMoveFrame(out MoveFrame moveFrame))
            return;

        if (!curveReachedEnd)
        {
            Vector2 endPoint = GetCurveEndPoint();

            float totalDistance = GetSafeDistance(Mathf.Abs(endPoint.x - airStartPosition.x));
            float xDirection = GetSignedDirection(endPoint.x - airStartPosition.x, state.MovementPlannedJumpDirection.x >= 0f ? 1f : -1f);
            float targetRise = endPoint.y - airStartPosition.y;

            Vector2 alongAxis = new Vector2(xDirection, 0f);
            Vector2 riseAxis = Vector2.up;

            TravelCurvePath(moveFrame, totalDistance, targetRise, alongAxis, riseAxis, true, true);
            return;
        }

        Vector2 upDirection = Vector2.up;
        Vector2 continuePosition = state.MovementRigidbody.position + upDirection * moveFrame.moveDistance;

        TravelAirborne(continuePosition, upDirection);
    }

    private void MoveAirborneCurveFromWall()
    {
        InitializeAirMoveRuntime(MovementRuntimeType.Curve);

        if (!TryBuildMoveFrame(out MoveFrame moveFrame))
            return;

        float verticalDistance = GetSafeDistance(state.MovementGetFloorToCeilingSpan());
        float verticalDirection = GetSignedDirection(
            state.MovementPlannedJumpDirection.y,
            state.MovementPlannedJumpSide == BounceStateBase<TEnemy>.BounceJumpSide.Right ? 1f : -1f
        );

        float outwardDirection = state.MovementCurrentSurface == BounceStateBase<TEnemy>.BounceSurface.RightWall ? -1f : 1f;
        float outwardDistance = GetSafeDistance(state.MovementPlannedJumpDistance);

        Vector2 alongAxis = new Vector2(0f, verticalDirection);
        Vector2 riseAxis = new Vector2(outwardDirection, 0f);

        TravelCurvePath(moveFrame, verticalDistance, outwardDistance, alongAxis, riseAxis, false, false);
    }

    private void MoveAirborneCurveFromCeiling()
    {
        InitializeAirMoveRuntime(MovementRuntimeType.Curve);

        if (!TryBuildMoveFrame(out MoveFrame moveFrame))
            return;

        if (!curveReachedEnd)
        {
            Vector2 floorEndPoint = GetCurveEndPoint();

            float totalDistance = GetSafeDistance(Mathf.Abs(floorEndPoint.x - airStartPosition.x));
            float xDirection = GetSignedDirection(floorEndPoint.x - airStartPosition.x, state.MovementPlannedJumpDirection.x >= 0f ? 1f : -1f);
            float targetDrop = Mathf.Abs(floorEndPoint.y - airStartPosition.y);

            Vector2 alongAxis = new Vector2(xDirection, 0f);
            Vector2 dropAxis = Vector2.down;

            TravelCurvePath(moveFrame, totalDistance, targetDrop, alongAxis, dropAxis, true, true);
            return;
        }

        float fallX = GetSignedDirection(
            state.MovementPlannedJumpDirection.x,
            state.MovementPlannedJumpSide == BounceStateBase<TEnemy>.BounceJumpSide.Right ? 1f : -1f
        );

        Vector2 fallDirection = new Vector2(fallX * 0.35f, -1f).normalized;
        Vector2 continuePosition = state.MovementRigidbody.position + fallDirection * moveFrame.moveDistance;

        TravelAirborne(continuePosition, fallDirection);
    }

    private void MoveAirborneCurveFromEdge()
    {
        InitializeAirMoveRuntime(MovementRuntimeType.Curve);

        if (!TryBuildMoveFrame(out MoveFrame moveFrame))
            return;

        Vector2 edgeDirection = state.MovementPlannedJumpDirection.sqrMagnitude > 0.0001f
            ? state.MovementPlannedJumpDirection.normalized
            : state.MovementPlannedArcAwayAxis.normalized;

        float xDirection = GetSignedDirection(edgeDirection.x, state.MovementPlannedJumpSide == BounceStateBase<TEnemy>.BounceJumpSide.Right ? 1f : -1f);
        float bendDirection = GetSignedDirection(edgeDirection.y, LandingIgnoreFailSafePathDirection.y >= 0f ? 1f : -1f);

        float totalDistance = GetSafeDistance(state.MovementPlannedJumpDistance);
        float targetBend = GetSafeDistance(state.MovementGetFloorToCeilingSpan());

        Vector2 alongAxis = new Vector2(xDirection, 0f);
        Vector2 bendAxis = new Vector2(0f, bendDirection);

        TravelCurvePath(moveFrame, totalDistance, targetBend, alongAxis, bendAxis, false, false);
    }

    #endregion

    #region Movement Data

    private enum MovementRuntimeType
    {
        Arc,
        Curve
    }

    private struct MoveFrame
    {
        public float moveSpeed;
        public float deltaTime;
        public Vector2 currentPosition;
        public float moveDistance;
    }

    internal float MoveSpeed()
    {
        return enemy.moveSpeed * enemy.moveSpeedMultiplier;
    }

    private bool TryBuildMoveFrame(out MoveFrame moveFrame)
    {
        moveFrame = default;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return false;

        float moveSpeed = MoveSpeed();

        moveFrame.moveSpeed = moveSpeed;
        moveFrame.deltaTime = deltaTime;
        moveFrame.currentPosition = state.MovementRigidbody.position;
        moveFrame.moveDistance = moveSpeed * deltaTime;

        return true;
    }

    #endregion

    #region Edge Movement Plan

    private void RequestEdgeBouncePlan(
        LandingProbeScan scan,
        Vector2 rawNormal,
        bool forceRebuild
    )
    {
        if (state.MovementPlannedAirMoveType != BounceStateBase<TEnemy>.BounceAirMoveType.Edge)
            edgeBouncePlanBuilt = false;

        if (forceRebuild)
            edgeBouncePlanBuilt = false;

        if (edgeBouncePlanBuilt)
            return;

        edgeBounceHitDirection = GetEdgeBounceHitDirection(scan);

        edgeBounceSurfaceNormal = rawNormal.sqrMagnitude > 0.0001f
            ? rawNormal.normalized
            : Vector2.zero;

        state.MovementPlannedAirMoveType = BounceStateBase<TEnemy>.BounceAirMoveType.Edge;
    }

    private void BuildEdgeBouncePlan()
    {
        PrepareEdgeSurfaceContact();
        LaunchFromEdge();

        edgeBouncePlanBuilt = true;
    }

    private void PrepareEdgeSurfaceContact()
    {
        state.MovementSetAttachedGravity();
        enemy.SetZeroVelocity();

        RotateFeetToEdgePoint();
    }

    private void RotateFeetToEdgePoint()
    {
        Vector2 snapSurfaceNormal = edgeBounceSurfaceNormal.sqrMagnitude > 0.0001f
            ? edgeBounceSurfaceNormal.normalized
            : state.MovementPlannedArcAwayAxis.normalized;

        if (snapSurfaceNormal.sqrMagnitude <= 0.0001f)
            snapSurfaceNormal = SurfaceNormal(state.MovementLaunchSurface).normalized;

        Vector2 feetTarget = -snapSurfaceNormal;
        float edgeSurfaceZ = Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = edgeSurfaceZ;
        enemy.transform.eulerAngles = eulerAngles;

        surfaceRotateVelocity = 0f;
        state.MovementPlannedArcStartZ = edgeSurfaceZ;

        if (ValidateEdgeFeetRotation())
            return;

        feetTarget = snapSurfaceNormal;
        edgeSurfaceZ = Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));

        eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = edgeSurfaceZ;
        enemy.transform.eulerAngles = eulerAngles;

        surfaceRotateVelocity = 0f;
        state.MovementPlannedArcStartZ = edgeSurfaceZ;

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

    private void LaunchFromEdge()
    {
        state.MovementSetAirborneGravity();

        Vector2 incomingDirection = GetEdgeIncomingDirection();
        Vector2 awayAxis = GetEdgeBounceAwayAxis(incomingDirection);

        state.MovementPlannedArcAwayAxis = awayAxis.sqrMagnitude > 0.0001f
            ? awayAxis.normalized
            : SurfaceNormal(state.MovementLaunchSurface).normalized;

        Vector2 beforeLaunchPosition = state.MovementRigidbody.position;
        Vector2 afterLaunchPosition = MoveEnemySlightlyOffEdge(state.MovementPlannedArcAwayAxis);

        StoreEdgeLaunchDirection(beforeLaunchPosition, afterLaunchPosition, state.MovementPlannedArcAwayAxis);
        ResetEdgeCurveTravelFromLaunch(afterLaunchPosition);
        StartEdgeSurfaceRelease();
    }

    private void StoreEdgeLaunchDirection(Vector2 beforeLaunchPosition, Vector2 afterLaunchPosition, Vector2 fallbackDirection)
    {
        Vector2 launchDirection = afterLaunchPosition - beforeLaunchPosition;

        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = fallbackDirection;

        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = state.MovementPlannedArcAwayAxis;

        if (launchDirection.sqrMagnitude <= 0.0001f)
            launchDirection = SurfaceNormal(state.MovementLaunchSurface);

        state.MovementPlannedJumpDirection = launchDirection.normalized;
        LandingIgnoreFailSafePathDirection = state.MovementPlannedJumpDirection;

        float originalDistance = GetSafeDistance(state.MovementPlannedJumpDistance);
        float maxEdgeDistance = originalDistance * 0.5f;
        float minEdgeDistance = maxEdgeDistance * 0.75f;

        state.MovementPlannedJumpDistance = UnityEngine.Random.Range(minEdgeDistance, maxEdgeDistance);

        Vector2 localRight = new Vector2(enemy.transform.right.x, enemy.transform.right.y).normalized;
        float localSideValue = Vector2.Dot(state.MovementPlannedJumpDirection, localRight);

        state.MovementPlannedJumpSide = localSideValue < 0f
            ? BounceStateBase<TEnemy>.BounceJumpSide.Left
            : BounceStateBase<TEnemy>.BounceJumpSide.Right;
    }

    private void StartEdgeSurfaceRelease()
    {
        CacheIgnoredSurfacesForEdgeRelease();

        edgeSurfaceReleaseActive = true;
        edgeSurfaceReleased = false;
    }

    private bool CheckEdgeSurfaceRelease()
    {
        if (edgeSurfaceReleased)
            return true;

        UpdateIgnoredSecondarySurface();

        edgeSurfaceReleaseActive = false;
        edgeSurfaceReleased = true;

        return true;
    }

    private Vector2 MoveEnemySlightlyOffEdge(Vector2 awayAxis)
    {
        Vector2 moveDirection = awayAxis.sqrMagnitude > 0.0001f
            ? awayAxis.normalized
            : GetEdgeIncomingDirection();

        float moveAmount = Mathf.Max(state.MovementGetSurfaceClearance(), MoveSpeed() * Time.deltaTime);
        Vector2 targetPosition = state.MovementRigidbody.position + moveDirection * moveAmount;

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
            awayAxis = state.MovementPlannedArcAwayAxis.sqrMagnitude > 0.0001f
                ? state.MovementPlannedArcAwayAxis.normalized
                : SurfaceNormal(state.MovementLaunchSurface).normalized;
        }

        awayAxis.Normalize();

        if (incomingDirection.sqrMagnitude > 0.0001f && Vector2.Dot(incomingDirection.normalized, awayAxis) > 0f)
            awayAxis = -awayAxis;

        return awayAxis.normalized;
    }

    private Vector2 GetEdgeIncomingDirection()
    {
        Vector2 incomingDirection = state.MovementRigidbody.linearVelocity;

        if (incomingDirection.sqrMagnitude > 0.0001f)
            return incomingDirection.normalized;

        incomingDirection = LandingIgnoreFailSafePathDirection;

        if (incomingDirection.sqrMagnitude > 0.0001f)
            return incomingDirection.normalized;

        incomingDirection = state.MovementPlannedJumpDirection + state.MovementPlannedArcAwayAxis;

        if (incomingDirection.sqrMagnitude > 0.0001f)
            return incomingDirection.normalized;

        return SurfaceNormal(state.MovementLaunchSurface).normalized;
    }

    private Vector2 GetEdgeBounceHitDirection(LandingProbeScan scan)
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

    #region Travel

    public void ApplyBounceMovement(Vector2 targetPosition, Vector2 pathDirection)
    {
        if (pathDirection.sqrMagnitude > 0.0001f)
            LandingIgnoreFailSafePathDirection = pathDirection.normalized;

        state.MovementRigidbody.MovePosition(targetPosition);
        state.MovementRigidbody.linearVelocity = pathDirection.normalized * MoveSpeed();
    }

    private void TravelAirborne(Vector2 targetPosition, Vector2 pathDirection)
    {
        ApplyBounceMovement(targetPosition, pathDirection);
        RotateAirborne(pathDirection);
    }

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

    #region Movement Helpers

    #region Initialize

    private void InitializeAirMoveRuntime(MovementRuntimeType runtimeType)
    {
        if (airMoveInitialized)
            return;

        ResetAirMoveRuntime(state.MovementRigidbody.position, true);

        if (runtimeType == MovementRuntimeType.Arc)
            arcReachedEnd = false;
        else
            curveReachedEnd = false;
    }

    private void ResetEdgeCurveTravelFromLaunch(Vector2 launchStartPosition)
    {
        ResetAirMoveRuntime(launchStartPosition, true);

        arcReachedEnd = false;
        curveReachedEnd = false;
    }

    private void ResetAirMoveRuntime(Vector2 startPosition, bool initialized)
    {
        airStartPosition = startPosition;
        airTravelDistance = 0f;
        curveTravelDistance = 0f;
        airMoveInitialized = initialized;
    }

    #endregion

    #region Progress

    private float AdvanceTravelDistance(
        float currentDistance,
        float totalDistance,
        float moveSpeed,
        float deltaTime,
        float slope,
        bool clampToDistance
    )
    {
        float distanceStep = moveSpeed * deltaTime / Mathf.Sqrt(1f + (slope * slope));
        float nextDistance = currentDistance + distanceStep;

        return clampToDistance
            ? Mathf.Min(totalDistance, nextDistance)
            : nextDistance;
    }

    private float GetProgress01(float travelDistance, float totalDistance)
    {
        return Mathf.Clamp01(travelDistance / Mathf.Max(0.001f, totalDistance));
    }

    #endregion

    #region Arc / Curve Build

    private void MoveArcPath(MoveFrame moveFrame, Vector2 alongAxis, Vector2 awayAxis)
    {
        if (!arcReachedEnd)
        {
            float totalDistance = GetSafeDistance(state.MovementPlannedJumpDistance);

            float slope = GetArcHeightSlope(airTravelDistance, totalDistance, state.MovementPlannedJumpHeight);
            airTravelDistance = AdvanceTravelDistance(
                airTravelDistance,
                totalDistance,
                moveFrame.moveSpeed,
                moveFrame.deltaTime,
                slope,
                true
            );

            float progress = GetProgress01(airTravelDistance, totalDistance);
            float heightAmount = Mathf.Sin(progress * Mathf.PI) * state.MovementPlannedJumpHeight;

            Vector2 targetPosition = airStartPosition
                                   + alongAxis * airTravelDistance
                                   + awayAxis * heightAmount;

            float tangentSlope = GetArcHeightSlope(airTravelDistance, totalDistance, state.MovementPlannedJumpHeight);
            Vector2 pathDirection = (alongAxis + awayAxis * tangentSlope).normalized;

            TravelAirborne(targetPosition, pathDirection);

            if (progress >= 1f)
                arcReachedEnd = true;

            return;
        }

        state.MovementRigidbody.linearVelocity = state.MovementRigidbody.linearVelocity;
    }

    private void TravelCurvePath(
        MoveFrame moveFrame,
        float totalDistance,
        float targetRise,
        Vector2 alongAxis,
        Vector2 riseAxis,
        bool clampToDistance,
        bool updateCurveReachedEnd
    )
    {
        float slope = GetCurveSlope(curveTravelDistance, totalDistance, targetRise);
        curveTravelDistance = AdvanceTravelDistance(
            curveTravelDistance,
            totalDistance,
            moveFrame.moveSpeed,
            moveFrame.deltaTime,
            slope,
            clampToDistance
        );

        float progress = GetProgress01(curveTravelDistance, totalDistance);
        float riseAmount = GetCurveRise(progress, targetRise);

        Vector2 targetPosition = airStartPosition
                               + alongAxis * curveTravelDistance
                               + riseAxis * riseAmount;

        float tangentSlope = GetCurveSlope(curveTravelDistance, totalDistance, targetRise);
        Vector2 pathDirection = (alongAxis + riseAxis * tangentSlope).normalized;

        TravelAirborne(targetPosition, pathDirection);

        if (updateCurveReachedEnd && progress >= 1f)
            curveReachedEnd = true;
    }

    private float GetArcHeightSlope(float travelDistance, float totalDistance, float maxHeight)
    {
        float progress = GetProgress01(travelDistance, totalDistance);
        return (Mathf.PI * maxHeight / Mathf.Max(0.001f, totalDistance)) * Mathf.Cos(progress * Mathf.PI);
    }

    private float GetCurveSlope(float travelDistance, float totalDistance, float targetRise)
    {
        float safeDistance = Mathf.Max(0.001f, totalDistance);
        float progress = GetProgress01(travelDistance, safeDistance);

        return (targetRise * (Mathf.PI * 0.5f) / safeDistance) * Mathf.Cos(progress * Mathf.PI * 0.5f);
    }

    private float GetCurveRise(float progress, float targetRise)
    {
        targetRise = Mathf.Max(0.001f, targetRise);
        return Mathf.Sin(progress * Mathf.PI * 0.5f) * targetRise;
    }

    private Vector2 GetCurveEndPoint()
    {
        float targetX = airStartPosition.x + (state.MovementPlannedJumpDirection.x * state.MovementPlannedJumpDistance);
        float targetY = airStartPosition.y + state.MovementGetFloorToCeilingSpan();

        return new Vector2(targetX, targetY);
    }

    #endregion

    #region Direction

    private float GetSignedDirection(float value, float fallback)
    {
        float direction = Mathf.Sign(value);

        if (Mathf.Abs(direction) <= 0.0001f)
            direction = fallback;

        return direction;
    }

    #endregion

    #region Distance

    private float GetSafeDistance(float distance)
    {
        return Mathf.Max(0.001f, distance);
    }

    #endregion

    #endregion

    #endregion

    #region Surface helpers
    public bool HasLeftCurrentSurface()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemy.GetWhatIsGround();
        filter.useTriggers = false;

        ContactPoint2D[] contacts = new ContactPoint2D[16];
        int count = state.MovementRigidbody.GetContacts(filter, contacts);

        if (count <= 0)
            return true;

        Vector2 currentNormal = SurfaceNormal(state.MovementCurrentSurface);

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
    public BounceStateBase<TEnemy>.BounceSurface ClassifySurface(Vector2 normal)
    {
        Vector2 normalizedNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;

        if (normalizedNormal.y > 0.7071f)
            return BounceStateBase<TEnemy>.BounceSurface.Floor;

        if (normalizedNormal.y < -0.7071f)
            return BounceStateBase<TEnemy>.BounceSurface.Ceiling;

        return (normalizedNormal.x < 0f) ? BounceStateBase<TEnemy>.BounceSurface.RightWall : BounceStateBase<TEnemy>.BounceSurface.LeftWall;
    }
    public Vector2 SurfaceNormal(BounceStateBase<TEnemy>.BounceSurface surface)
    {
        switch (surface)
        {
            case BounceStateBase<TEnemy>.BounceSurface.Floor:
                return Vector2.up;

            case BounceStateBase<TEnemy>.BounceSurface.Ceiling:
                return Vector2.down;

            case BounceStateBase<TEnemy>.BounceSurface.RightWall:
                return Vector2.left;

            case BounceStateBase<TEnemy>.BounceSurface.LeftWall:
                return Vector2.right;
        }

        return Vector2.up;
    }
    public void AlignToSurfaceNormal(Vector2 normal)
    {
        state.MovementCurrentSurfaceNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;
    }
    public float GetSurfaceTargetZ(BounceStateBase<TEnemy>.BounceSurface surface)
    {
        Vector2 surfaceNormal = SurfaceNormal(surface);

        Vector2 feetTarget = -surfaceNormal;
        return Normalize360(Vector2.SignedAngle(Vector2.down, feetTarget));
    }
    public void RotateFeetToSurface()
    {
        float targetZ = GetSurfaceTargetZ(state.MovementCurrentSurface);

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
    public bool IsFeetAlignedToSurface()
    {
        float targetZ = GetSurfaceTargetZ(state.MovementCurrentSurface);
        float currentZ = enemy.transform.eulerAngles.z;

        return Mathf.Abs(Mathf.DeltaAngle(currentZ, targetZ)) <= SURFACE_ALIGN_EPS_DEG;
    }
    #endregion


    #region General helpers

    private float Normalize360(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    #endregion
}
