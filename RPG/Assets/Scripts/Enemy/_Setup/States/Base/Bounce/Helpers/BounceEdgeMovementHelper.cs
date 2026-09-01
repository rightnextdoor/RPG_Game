using System.Collections.Generic;
using UnityEngine;

public partial class BounceMovementHandler<TEnemy> where TEnemy : Enemy_Regular
{
    #region Edge Movement Plan

    private void RequestEdgeBouncePlan(
        LandingProbeScan scan,
        Vector2 rawNormal,
        bool forceRebuild
    )
    {
        if (state.MovementPlannedAirMoveType != BounceAirMoveType.Edge)
            edgeBouncePlanBuilt = false;

        if (forceRebuild)
            edgeBouncePlanBuilt = false;

        if (edgeBouncePlanBuilt)
            return;

        edgeBounceHitDirection = GetEdgeBounceHitDirection(scan);

        edgeBounceSurfaceNormal = rawNormal.sqrMagnitude > 0.0001f
            ? rawNormal.normalized
            : Vector2.zero;

        state.MovementPlannedAirMoveType = BounceAirMoveType.Edge;
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
            ? BounceJumpSide.Left
            : BounceJumpSide.Right;
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
}
