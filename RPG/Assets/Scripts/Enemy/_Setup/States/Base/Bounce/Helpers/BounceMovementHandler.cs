using System.Collections.Generic;
using UnityEngine;

public partial class BounceMovementHandler<TEnemy> where TEnemy : Enemy_Regular
{
    private readonly BounceStateBase<TEnemy> state;
    private readonly TEnemy enemy;

    public BounceMovementHandler(BounceStateBase<TEnemy> state, TEnemy enemy)
    {
        this.state = state;
        this.enemy = enemy;
    }

    #region Secondary surface runtime

    private readonly List<BounceSurface> ignoredLaunchSurfaces = new();
    private readonly List<BounceSurface> ignoredLaunchEdgeSurfaces = new();

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
}
