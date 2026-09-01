using System.Collections.Generic;
using UnityEngine;

public partial class BounceMovementHandler<TEnemy> where TEnemy : Enemy_Regular
{
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

            BounceSurface rawSurface = ClassifySurface(rawNormal);

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
        BounceSurface rawSurface,
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
                    state.MovementPlannedAirMoveType != BounceAirMoveType.Edge ||
                    !edgeBouncePlanBuilt ||
                    previousEdgePoint != detectedEdgePoint;

                RequestEdgeBouncePlan(scan, rawNormal, forceRebuild);
            }

            return false;
        }

        BounceSurface resolvedSurface = ResolveSurfaceFromProbeScanDefault(rawSurface, scan);

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

    internal BounceSurface ResolveSurfaceFromProbeScanDefault(
        BounceSurface rawSurface,
        LandingProbeScan scan
    )
    {
        if (TryResolveFlatProbeSurface(scan, out BounceSurface flatSurface))
            return flatSurface;

        return ResolveSurfaceByProbeScore(rawSurface, scan);
    }

    private bool TryResolveFlatProbeSurface(
        LandingProbeScan scan,
        out BounceSurface surface
    )
    {
        surface = BounceSurface.Floor;

        bool floorFlat = scan.down && scan.downLeft && scan.downRight;
        bool ceilingFlat = scan.up && scan.upLeft && scan.upRight;
        bool leftWallFlat = scan.left && scan.upLeft && scan.downLeft;
        bool rightWallFlat = scan.right && scan.upRight && scan.downRight;

        int flatCount = 0;

        if (floorFlat)
        {
            surface = BounceSurface.Floor;
            flatCount++;
        }

        if (ceilingFlat)
        {
            surface = BounceSurface.Ceiling;
            flatCount++;
        }

        if (leftWallFlat)
        {
            surface = BounceSurface.LeftWall;
            flatCount++;
        }

        if (rightWallFlat)
        {
            surface = BounceSurface.RightWall;
            flatCount++;
        }

        return flatCount == 1;
    }

    private BounceSurface ResolveSurfaceByProbeScore(
        BounceSurface rawSurface,
        LandingProbeScan scan
    )
    {
        int floorScore = GetProbeScore(BounceSurface.Floor, scan);
        int ceilingScore = GetProbeScore(BounceSurface.Ceiling, scan);
        int leftWallScore = GetProbeScore(BounceSurface.LeftWall, scan);
        int rightWallScore = GetProbeScore(BounceSurface.RightWall, scan);

        int bestScore = Mathf.Max(
            Mathf.Max(floorScore, ceilingScore),
            Mathf.Max(leftWallScore, rightWallScore)
        );

        if (bestScore <= 0)
            return rawSurface;

        if (GetProbeScore(rawSurface, scan) == bestScore)
            return rawSurface;

        BounceSurface movementSurface = GetSurfaceFromAirVelocity();
        if (GetProbeScore(movementSurface, scan) == bestScore)
            return movementSurface;

        if (floorScore == bestScore)
            return BounceSurface.Floor;

        if (ceilingScore == bestScore)
            return BounceSurface.Ceiling;

        if (leftWallScore == bestScore)
            return BounceSurface.LeftWall;

        return BounceSurface.RightWall;
    }

    private int GetProbeScore(
        BounceSurface surface,
        LandingProbeScan scan
    )
    {
        switch (surface)
        {
            case BounceSurface.Floor:
                return (scan.down ? 4 : 0) +
                       (scan.downLeft ? 1 : 0) +
                       (scan.downRight ? 1 : 0);

            case BounceSurface.Ceiling:
                return (scan.up ? 4 : 0) +
                       (scan.upLeft ? 1 : 0) +
                       (scan.upRight ? 1 : 0);

            case BounceSurface.LeftWall:
                return (scan.left ? 4 : 0) +
                       (scan.upLeft ? 1 : 0) +
                       (scan.downLeft ? 1 : 0);

            case BounceSurface.RightWall:
                return (scan.right ? 4 : 0) +
                       (scan.upRight ? 1 : 0) +
                       (scan.downRight ? 1 : 0);
        }

        return 0;
    }

    private BounceSurface GetSurfaceFromAirVelocity()
    {
        Vector2 velocity = state.MovementRigidbody.linearVelocity;

        if (velocity.sqrMagnitude <= 0.0001f)
            return state.MovementCurrentSurface;

        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
        {
            return velocity.x < 0f
                ? BounceSurface.LeftWall
                : BounceSurface.RightWall;
        }

        return velocity.y > 0f
            ? BounceSurface.Ceiling
            : BounceSurface.Floor;
    }

    private bool TryGetProbePointForSurface(
        LandingProbeScan scan,
        BounceSurface surface,
        out Vector2 point
    )
    {
        point = default;

        switch (surface)
        {
            case BounceSurface.Floor:
                if (scan.down) { point = scan.downHit.point; return true; }
                if (scan.downLeft) { point = scan.downLeftHit.point; return true; }
                if (scan.downRight) { point = scan.downRightHit.point; return true; }
                break;

            case BounceSurface.Ceiling:
                if (scan.up) { point = scan.upHit.point; return true; }
                if (scan.upLeft) { point = scan.upLeftHit.point; return true; }
                if (scan.upRight) { point = scan.upRightHit.point; return true; }
                break;

            case BounceSurface.LeftWall:
                if (scan.left) { point = scan.leftHit.point; return true; }
                if (scan.upLeft) { point = scan.upLeftHit.point; return true; }
                if (scan.downLeft) { point = scan.downLeftHit.point; return true; }
                break;

            case BounceSurface.RightWall:
                if (scan.right) { point = scan.rightHit.point; return true; }
                if (scan.upRight) { point = scan.upRightHit.point; return true; }
                if (scan.downRight) { point = scan.downRightHit.point; return true; }
                break;
        }

        return false;
    }

    #endregion

    #endregion
}
