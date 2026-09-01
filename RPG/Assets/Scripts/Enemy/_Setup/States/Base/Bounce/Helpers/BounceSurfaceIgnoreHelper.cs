using System.Collections.Generic;
using UnityEngine;

public partial class BounceMovementHandler<TEnemy> where TEnemy : Enemy_Regular
{
    #region Surface Ignored

    #region Secondary surface ignore

    public void CacheIgnoredSecondarySurface()
    {
        ignoredLaunchSurfaces.Clear();
        ignoredLaunchEdgeSurfaces.Clear();

        ProbeIgnoredLaunchSurface(BounceJumpSide.Left);
        ProbeIgnoredLaunchSurface(BounceJumpSide.Right);
        ProbeIgnoredLaunchEdgeSurface(BounceJumpSide.Left);
        ProbeIgnoredLaunchEdgeSurface(BounceJumpSide.Right);

        ResetLandingIgnoreFailSafeTracking();
    }

    public void UpdateIgnoredSecondarySurface()
    {
        for (int i = ignoredLaunchSurfaces.Count - 1; i >= 0; i--)
        {
            BounceSurface surface = ignoredLaunchSurfaces[i];

            bool stillOnLeft = IsTouchingSurface(BounceJumpSide.Left, surface);
            bool stillOnRight = IsTouchingSurface(BounceJumpSide.Right, surface);

            if (!stillOnLeft && !stillOnRight)
                ignoredLaunchSurfaces.RemoveAt(i);
        }

        for (int i = ignoredLaunchEdgeSurfaces.Count - 1; i >= 0; i--)
        {
            BounceSurface surface = ignoredLaunchEdgeSurfaces[i];

            bool stillOnLeft = IsTouchingSurface(BounceJumpSide.Left, surface);
            bool stillOnRight = IsTouchingSurface(BounceJumpSide.Right, surface);

            bool stillOnLeftEdge = IsLaunchEdgeProbeHit(BounceJumpSide.Left, surface);
            bool stillOnRightEdge = IsLaunchEdgeProbeHit(BounceJumpSide.Right, surface);

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
        BounceJumpSide jumpSide,
        BounceSurface surface
    )
    {
        Vector2 direction = state.MovementGetLocalJumpDirection(jumpSide).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        if (!ProbeSurface(direction, out BounceSurface hitSurface))
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

    private void ProbeIgnoredLaunchSurface(BounceJumpSide jumpSide)
    {
        Vector2 direction = state.MovementGetLocalJumpDirection(jumpSide).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        if (!ProbeSurface(direction, out BounceSurface hitSurface))
            return;

        if (hitSurface == state.MovementCurrentSurface)
            return;

        if (ignoredLaunchSurfaces.Contains(hitSurface))
            return;

        ignoredLaunchSurfaces.Add(hitSurface);
    }

    private void ProbeIgnoredLaunchEdgeSurface(BounceJumpSide jumpSide)
    {
        if (!ProbeLaunchEdgeSurface(jumpSide, out BounceSurface hitSurface))
            return;

        if (hitSurface == state.MovementCurrentSurface)
            return;

        if (ignoredLaunchEdgeSurfaces.Contains(hitSurface))
            return;

        ignoredLaunchEdgeSurfaces.Add(hitSurface);
    }

    private bool IsLaunchEdgeProbeHit(
        BounceJumpSide jumpSide,
        BounceSurface surface
    )
    {
        if (!ProbeLaunchEdgeSurface(jumpSide, out BounceSurface hitSurface))
            return false;

        return hitSurface == surface;
    }

    private bool ProbeLaunchEdgeSurface(
        BounceJumpSide jumpSide,
        out BounceSurface hitSurface
    )
    {
        hitSurface = BounceSurface.Floor;

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
        out BounceSurface hitSurface
    )
    {
        hitSurface = BounceSurface.Floor;

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

    private bool IsLandingSurfaceIgnored(BounceSurface surface)
    {
        return ignoredLaunchSurfaces.Contains(surface) ||
               ignoredLaunchEdgeSurfaces.Contains(surface);
    }

    private void CacheIgnoredSurfacesFromEdgePoint()
    {
        switch (edgeBouncePoint)
        {
            case BounceEdgePoint.UpLeft:
                AddIgnoredLaunchEdgeSurface(BounceSurface.Floor);
                AddIgnoredLaunchEdgeSurface(BounceSurface.RightWall);
                break;

            case BounceEdgePoint.UpRight:
                AddIgnoredLaunchEdgeSurface(BounceSurface.Floor);
                AddIgnoredLaunchEdgeSurface(BounceSurface.LeftWall);
                break;

            case BounceEdgePoint.DownLeft:
                AddIgnoredLaunchEdgeSurface(BounceSurface.Ceiling);
                AddIgnoredLaunchEdgeSurface(BounceSurface.RightWall);
                break;

            case BounceEdgePoint.DownRight:
                AddIgnoredLaunchEdgeSurface(BounceSurface.Ceiling);
                AddIgnoredLaunchEdgeSurface(BounceSurface.LeftWall);
                break;
        }
    }

    private void AddIgnoredLaunchEdgeSurface(BounceSurface surface)
    {
        if (ignoredLaunchEdgeSurfaces.Contains(surface))
            return;

        ignoredLaunchEdgeSurfaces.Add(surface);
    }

    #endregion

    #endregion
}
