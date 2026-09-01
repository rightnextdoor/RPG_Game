using System.Collections.Generic;
using UnityEngine;

public partial class BounceMovementHandler<TEnemy> where TEnemy : Enemy_Regular
{
    #region Movement

    #region Main Movement

    public void MoveAirborne()
    {
        switch (state.MovementPlannedAirMoveType)
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
            state.MovementPlannedJumpSide == BounceJumpSide.Right ? 1f : -1f
        );

        float outwardDirection = state.MovementCurrentSurface == BounceSurface.RightWall ? -1f : 1f;
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
            state.MovementPlannedJumpSide == BounceJumpSide.Right ? 1f : -1f
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

        float xDirection = GetSignedDirection(edgeDirection.x, state.MovementPlannedJumpSide == BounceJumpSide.Right ? 1f : -1f);
        float bendDirection = GetSignedDirection(edgeDirection.y, LandingIgnoreFailSafePathDirection.y >= 0f ? 1f : -1f);

        float totalDistance = GetSafeDistance(state.MovementPlannedJumpDistance);
        float targetBend = GetSafeDistance(state.MovementGetFloorToCeilingSpan());

        Vector2 alongAxis = new Vector2(xDirection, 0f);
        Vector2 bendAxis = new Vector2(0f, bendDirection);

        TravelCurvePath(moveFrame, totalDistance, targetBend, alongAxis, bendAxis, false, false);
    }

    #endregion

    #region Movement Data

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
