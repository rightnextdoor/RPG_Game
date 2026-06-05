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

            BounceStateBase<TEnemy>.BounceSurface rawSurface = state.MovementClassifySurface(rawNormal);

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
        resolvedNormal = state.MovementSurfaceNormal(rawSurface);

        if (!ScanLandingProbes(out LandingProbeScan scan))
            return false;

        if (!HasMainLandingProbeHit(scan))
        {
            if (HasDiagonalLandingProbeHit(scan))
            {
                BounceStateBase<TEnemy>.BounceEdgePoint previousEdgePoint = state.MovementEdgeBouncePoint;
                BounceStateBase<TEnemy>.BounceEdgePoint detectedEdgePoint = GetEdgeBouncePointFromProbeScan(scan);

                state.MovementEdgeBouncePoint = detectedEdgePoint;
                state.MovementEdgeBounceProbeHitPoint = GetEdgeBounceProbeHitPointFromProbeScan(scan);

                bool forceRebuild =
                    state.MovementPlannedAirMoveType != BounceStateBase<TEnemy>.BounceAirMoveType.Edge ||
                    !state.MovementEdgeBouncePlanBuilt ||
                    previousEdgePoint != detectedEdgePoint;

                state.MovementRequestEdgeBouncePlan(scan, rawNormal, forceRebuild);
            }

            return false;
        }

        BounceStateBase<TEnemy>.BounceSurface resolvedSurface = state.MovementResolveSurfaceFromProbeScan(rawSurface, scan);

        if (IsLandingSurfaceIgnored(resolvedSurface))
        {
            return false;
        }

        if (TryGetProbePointForSurface(scan, resolvedSurface, out Vector2 probePoint))
            resolvedPoint = probePoint;

        resolvedNormal = state.MovementSurfaceNormal(resolvedSurface);
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
        switch (state.MovementEdgeBouncePoint)
        {
            case BounceStateBase<TEnemy>.BounceEdgePoint.UpLeft:
                return scan.downRightHit.point;

            case BounceStateBase<TEnemy>.BounceEdgePoint.UpRight:
                return scan.downLeftHit.point;

            case BounceStateBase<TEnemy>.BounceEdgePoint.DownLeft:
                return scan.upRightHit.point;

            case BounceStateBase<TEnemy>.BounceEdgePoint.DownRight:
                return scan.upLeftHit.point;
        }

        return Vector2.zero;
    }

    private BounceStateBase<TEnemy>.BounceEdgePoint GetEdgeBouncePointFromProbeScan(LandingProbeScan scan)
    {
        if (scan.upLeft)
            return BounceStateBase<TEnemy>.BounceEdgePoint.DownRight;

        if (scan.upRight)
            return BounceStateBase<TEnemy>.BounceEdgePoint.DownLeft;

        if (scan.downLeft)
            return BounceStateBase<TEnemy>.BounceEdgePoint.UpRight;

        if (scan.downRight)
            return BounceStateBase<TEnemy>.BounceEdgePoint.UpLeft;

        return BounceStateBase<TEnemy>.BounceEdgePoint.None;
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

    public void StartEdgeSurfaceRelease()
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
        float expectedMove = state.MovementMoveSpeed() * deltaTime;
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

        hitSurface = state.MovementClassifySurface(hit.normal);
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
        switch (state.MovementEdgeBouncePoint)
        {
            case BounceStateBase<TEnemy>.BounceEdgePoint.UpLeft:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Floor);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.RightWall);
                break;

            case BounceStateBase<TEnemy>.BounceEdgePoint.UpRight:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Floor);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.LeftWall);
                break;

            case BounceStateBase<TEnemy>.BounceEdgePoint.DownLeft:
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.Ceiling);
                AddIgnoredLaunchEdgeSurface(BounceStateBase<TEnemy>.BounceSurface.RightWall);
                break;

            case BounceStateBase<TEnemy>.BounceEdgePoint.DownRight:
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
}