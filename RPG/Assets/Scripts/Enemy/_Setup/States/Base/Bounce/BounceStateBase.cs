using System.Collections.Generic;
using UnityEngine;

public class BounceStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private static readonly List<StateSound> Empty = new();

    protected readonly List<StateSound> enterSounds;
    protected readonly List<StateSound> exitSounds;

    #region Bounce runtime

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
        Airborne
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

    protected BouncePhase currentPhase = BouncePhase.Attach;

    protected bool isAttached;
    protected bool isAirborne;

    protected BounceSurface currentSurface = BounceSurface.Floor;
    protected Vector2 currentSurfaceNormal = Vector2.up;

    protected float bounceTimer;
    protected bool timerSet;
    protected bool launchReady;

    protected float savedDefaultGravity;
    protected bool gravityCached;

    protected float surfaceTargetZ;
    protected float surfaceRotateVelocity;
    protected const float SURFACE_ALIGN_TIME = 0.06f;
    protected const float SURFACE_ALIGN_EPS_DEG = 0.75f;

    protected RaycastHit2D landingHit;
    protected bool hasLandingHit;

    protected const float AIRBORNE_ROTATE_SPEED = 720f;
    protected const float LANDING_NORMAL_MIN_DOT = 0.1f;

    protected Vector2 launchVelocity;
    protected bool needsPatrolReturn;

    #endregion

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

    #region State

    public override void Enter()
    {
        base.Enter();
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
        }
    }

    #endregion

    #region Bounce phases

    protected virtual void AttachPhase()
    {
        SurfacePhase();

        if (!IsFeetAlignedToSurface())
            return;

        hasLandingHit = false;

        if (stateMachine.currentState == this && enemy.stateMachine.currentState == this)
            return;
    }

    protected virtual void SurfacePhase()
    {
        SetAttachedGravity();
        RotateFeetToSurface();
    }

    protected virtual void LaunchPhase()
    {
        SetAirborneGravity();
        MoveEnemy(launchVelocity);
        currentPhase = BouncePhase.Airborne;
    }

    protected virtual void AirbornePhase()
    {
        RotateInMovementDirection();

        if (!TryFindLandingSurface(out RaycastHit2D bestHit))
            return;

        landingHit = bestHit;
        hasLandingHit = true;

        Vector2 hitNormal = bestHit.normal;
        currentSurface = ClassifySurface(hitNormal);
        AlignToSurfaceNormal(hitNormal);

        Debug.Log(
            $"{enemy.GetType().Name} airborne hit surface. " +
            $"velocity={rb.linearVelocity} normal={hitNormal} surface={currentSurface}"
        );

        currentPhase = BouncePhase.Attach;
    }

    #endregion

    #region Jump planning

    protected virtual void PlanJump()
    {
        BounceJumpType jumpType = GetWeightedJumpType();
        BounceJumpSide jumpSide = GetJumpSide();
        float jumpDistance = GetJumpDistance(jumpSide);
        float jumpHeight = GetJumpHeight(jumpType);

        BuildLaunchVelocity(jumpSide, jumpDistance, jumpHeight);
    }

    #region Jump helpers
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

    protected virtual float GetJumpHeight(BounceJumpType jumpType)
    {
        GetJumpHeightRange(jumpType, out float minHeight, out float maxHeight);
        return UnityEngine.Random.Range(minHeight, maxHeight);
    }

    protected virtual void GetJumpHeightRange(BounceJumpType jumpType, out float minHeight, out float maxHeight)
    {
        minHeight = GetPlayerHeight();
        maxHeight = Mathf.Max(minHeight, enemy.patrolAreaSize.y);

        bool useFloorPadding =
            currentSurface == BounceSurface.Floor &&
            jumpType == BounceJumpType.Normal;

        if (useFloorPadding)
            maxHeight -= Mathf.Max(0f, enemy.bounceJumpHeightPadding);

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

    protected virtual void BuildLaunchVelocity(BounceJumpSide jumpSide, float distance, float height)
    {
        Vector2 dir = GetLocalJumpDirection(jumpSide);

        float timeToPeak = Mathf.Max(0.05f, height / Mathf.Max(0.1f, Mathf.Abs(Physics2D.gravity.y)));

        float vx = (distance / Mathf.Max(0.05f, timeToPeak)) * dir.normalized.x;
        float vy = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * height);

        launchVelocity = new Vector2(vx, vy);
    }

    protected virtual bool IsAtLeftEdge()
    {
        float edgePadding = GetEnemyWidth() * 0.5f;
        return enemy.transform.position.x <= enemy.patrolLeftX + edgePadding;
    }

    protected virtual bool IsAtRightEdge()
    {
        float edgePadding = GetEnemyWidth() * 0.5f;
        return enemy.transform.position.x >= enemy.patrolRightX - edgePadding;
    }

    #endregion

    #endregion

    #region Helpers

    #region Move
    protected virtual void MoveEnemy(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    #endregion

    #region Surface
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
        eulerAngles.z = Mathf.SmoothDampAngle(
            eulerAngles.z,
            targetZ,
            ref surfaceRotateVelocity,
            SURFACE_ALIGN_TIME
        );

        enemy.transform.eulerAngles = eulerAngles;
    }
    protected virtual bool IsFeetAlignedToSurface()
    {
        float targetZ = GetSurfaceTargetZ(currentSurface);
        float currentZ = enemy.transform.eulerAngles.z;

        return Mathf.Abs(Mathf.DeltaAngle(currentZ, targetZ)) <= SURFACE_ALIGN_EPS_DEG;
    }
    #endregion

    #region Airborne
    protected virtual bool TryFindLandingSurface(out RaycastHit2D bestHit)
    {
        bestHit = default;

        Collider2D myCollider = enemy.GetComponent<Collider2D>();
        if (myCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemy.GetWhatIsGround();
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[8];
        int count = myCollider.Cast(rb.linearVelocity.normalized, filter, hits, 0.05f);

        if (count <= 0)
            return false;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude <= 0.0001f)
            velocity = Vector2.down;

        Vector2 incoming = -velocity.normalized;

        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null)
                continue;

            float score = Vector2.Dot(hit.normal.normalized, incoming);
            if (score < LANDING_NORMAL_MIN_DOT)
                continue;

            if (!found || score > bestScore)
            {
                bestScore = score;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }
    protected virtual void RotateInMovementDirection()
    {
        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude <= 0.0001f)
            return;

        float targetZ = Normalize360(Vector2.SignedAngle(Vector2.down, -velocity.normalized));

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = Mathf.MoveTowardsAngle(
            eulerAngles.z,
            targetZ,
            AIRBORNE_ROTATE_SPEED * Time.deltaTime
        );

        enemy.transform.eulerAngles = eulerAngles;
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