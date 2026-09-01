using System.Collections.Generic;
using UnityEngine;

public partial class BounceStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    #region Jump planning

    protected virtual void PlanJump()
    {
        plannedJumpSide = GetJumpSide();
        plannedJumpDirection = GetLocalJumpDirection(plannedJumpSide).normalized;
        plannedJumpDistance = GetJumpDistance(plannedJumpSide);
        plannedJumpType = GetWeightedJumpType();
        plannedJumpHeight = GetJumpHeight(plannedJumpType);

        launchSurface = currentSurface;
        plannedArcAwayAxis = movementHandler.SurfaceNormal(launchSurface).normalized;
        plannedArcStartZ = movementHandler.GetSurfaceTargetZ(launchSurface);

        plannedAirMoveType = GetAirMoveType(launchSurface, plannedJumpType);

        movementHandler.ResetMovementRuntimeForNewPlan(enemy.transform.position);

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
}
