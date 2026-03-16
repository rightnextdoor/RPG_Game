using UnityEngine;
using static Enemy_Regular;

public class MoveStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private readonly System.Func<EnemyState> idleState;
    private readonly System.Func<EnemyState> battleState;

    private float traverseStopTimer;
    private bool needsPatrolReturn;

    #region Surface crawler info

    private enum Surface4 { Floor, RightWall, Ceiling, LeftWall }

    private struct CrawlStep
    {
        public Surface4 surface;
        public int dir;
        public CrawlStep(Surface4 s, int d) { surface = s; dir = (d >= 0) ? +1 : -1; }
    }

    private CrawlStep step;

    private Vector2 currentNormal = Vector2.up;
    private float savedGravity;

    private int crawlSense = +1;

    private float zSmoothVel;
    private const float Z_ALIGN_TIME = 0.06f;
    private const float VEL_BLEND_HZ = 12f;

    private bool wallAlignActive;
    private float wallAlignTargetZ;
    private const float WALL_ALIGN_EPS_DEG = 0.75f;

    private bool edgeWrapActive;

    #endregion

    #region Edge wrap info

    private CrawlStep edgeNextStep;
    private float edgeTargetZ;

    private const float EDGE_ATTACH_CAST_EXTRA = 0.15f;

    private enum EdgeWrapPhase { None, RotateAtFeet, NudgeClear, OffsetToNextSurface, Confirm }
    private EdgeWrapPhase edgeWrapPhase = EdgeWrapPhase.None;

    private CrawlStep edgeWrapFromStep;
    private float edgeWrapWidth;
    private Vector2 edgeWrapFeetLocal;
    private Vector2 edgeWrapFeetWorldPinned;
    private CapsuleCollider2D edgeWrapCap;

    private Vector2 edgeRotateStartPos;
    private Vector2 edgeRotateTargetPos;
    private float edgeRotateStartZ;
    private float edgeRotateTargetZ;
    private float edgeRotateMoveSpeed;
    private float edgeRotateTurnSpeed;

    private Vector2 edgeOffsetStartPos;
    private Vector2 edgeOffsetTargetPos;
    private float edgeOffsetMoveSpeed;

    private float edgeNudgeMoveSpeed;

    #region Edge debug freeze

    // Freeze for debugging the rotation of the enemy.
    private bool edgeFreezeActive;
    private bool edgeFreezeEnabled = false;
    private Surface4 edgeFreezeSurface = Surface4.Floor;

    #endregion

    #endregion

    public MoveStateBase(
        TEnemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        System.Func<EnemyState> idleState,
        System.Func<EnemyState> battleState
    ) : base(enemyBase, stateMachine, animBoolName)
    {
        this.idleState = idleState;
        this.battleState = battleState;
    }

    public override void Enter()
    {
        base.Enter();

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            needsPatrolReturn = false;
            savedGravity = rb.gravityScale;
            rb.gravityScale = 0f;

            currentNormal = enemy.transform.up;

            TryRandomizeFacingOnEnter();

            AlignToNormal(currentNormal);

            step = new CrawlStep(ClassifySurface(currentNormal), (enemy.facingDir >= 0) ? +1 : -1);
            ApplyStepCrawlSense();
            EnsureFacingMatchesStep();
            edgeTargetZ = 0f;
            ResetEdgeWrapState();

            return;
        }

        bool usesPatrolBox =
            enemy.moveMode == RegularMoveMode.PatrolArea ||
            enemy.moveMode == RegularMoveMode.TraverseNoStops;

        if (usesPatrolBox && IsOutsidePatrolX(enemy.transform.position.x))
        {
            needsPatrolReturn = true;
        }
        else
        {
            needsPatrolReturn = false;
            TryRandomizeFacingOnEnter();
        }

        if (enemy.moveMode == RegularMoveMode.TraverseNoStops)
        {
            float minStopSeconds = Mathf.Max(0f, enemy.traverseStopMinSeconds);
            float maxStopSeconds = Mathf.Max(minStopSeconds, enemy.traverseStopMaxSeconds);
            traverseStopTimer = (maxStopSeconds > 0f) ? Random.Range(minStopSeconds, maxStopSeconds) : 0f;
        }

        ApplyVelocity();
    }

    public override void Update()
    {
        base.Update();

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            SurfaceCrawler();
            return;
        }

        if (battleState != null && PlayerDetectedValid())
        {
            stateMachine.ChangeState(battleState());
            return;
        }

        if (needsPatrolReturn)
        {
            needsPatrolReturn = ReturnToPatrolCenterIfOutside();
            return;
        }

        switch (enemy.moveMode)
        {
            case RegularMoveMode.PatrolArea:
                PatrolArea();
                break;

            case RegularMoveMode.TraverseNoStops:
                Traverse();
                break;
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            rb.gravityScale = savedGravity;
            currentNormal = Vector2.up;
            ResetEdgeWrapState();
        }
    }

    #region Mode methods

    private void PatrolArea()
    {
        ApplyVelocity();
        if (IsBoundaryAheadInFacingDir() || enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            OnBoundary();
            return;
        }
    }

    private void Traverse()
    {
        ApplyVelocity();

        if (IsBoundaryAheadInFacingDir() || enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.Flip();
            ApplyVelocity();
            return;
        }

        if (traverseStopTimer > 0f)
        {
            traverseStopTimer -= Time.deltaTime;
            if (traverseStopTimer <= 0f && idleState != null)
            {
                stateMachine.ChangeState(idleState());
                return;
            }
        }
    }

    private void SurfaceCrawler()
    {
        if (edgeFreezeActive)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (edgeWrapActive)
        {
            UpdateEdgeWrap();
            return;
        }

        SmoothAlignToCurrentNormal();
        ApplyVelocityCrawler();

        bool wallHit = LocalWallProbe(out var _);
        if (wallHit)
        {
            var nextStep = AdvanceOnWallHit(step);
            SetStepImmediate(nextStep);
            BeginWallAlign();
            return;
        }

        if (wallAlignActive)
            return;

        bool edgeHit = LocalEdgeProbe();
        if (edgeHit)
        {
            DoEdgeOffsetWrap(step);
            UpdateEdgeWrap();
            return;
        }
    }

    #endregion

    #region Patrol boundary

    private bool ReturnToPatrolCenterIfOutside()
    {
        float currentX = enemy.transform.position.x;
        if (!IsOutsidePatrolX(currentX))
            return false;

        int desiredDir = (enemy.patrolCenter.x - currentX) >= 0f ? +1 : -1;
        if (enemy.facingDir != desiredDir)
            enemy.Flip();

        ApplyVelocity();

        const float centerSnapEpsilon = 0.1f;
        return Mathf.Abs(enemy.patrolCenter.x - currentX) > centerSnapEpsilon;
    }

    private bool IsBoundaryAheadInFacingDir()
    {
        float currentX = enemy.transform.position.x;

        return (enemy.facingDir > 0 && currentX >= enemy.patrolRightX)
            || (enemy.facingDir < 0 && currentX <= enemy.patrolLeftX);
    }

    private bool IsOutsidePatrolX(float x) => x < enemy.patrolLeftX || x > enemy.patrolRightX;

    private void OnBoundary()
    {
        enemy.boundaryTouchedOnMove = true;

        if (idleState != null)
        {
            stateMachine.ChangeState(idleState());
        }
        else
        {
            enemy.Flip();
            ApplyVelocity();
        }
    }

    #endregion

    #region Velocity

    private void ApplyVelocity()
    {
        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            ApplyVelocityCrawler();
            return;
        }

        float xVelocity = enemy.moveSpeed * enemy.moveSpeedMultiplier * enemy.facingDir;
        enemy.SetVelocity(xVelocity, rb.linearVelocity.y);
    }

    private void ApplyVelocityCrawler()
    {
        float speed = enemy.moveSpeed * enemy.moveSpeedMultiplier;

        Vector2 tangent = new Vector2(currentNormal.y, -currentNormal.x) * crawlSense;
        Vector2 targetVelocity = tangent.normalized * speed;

        float alpha = 1f - Mathf.Exp(-VEL_BLEND_HZ * Time.deltaTime);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, alpha);
    }

    #endregion

    #region Surface crawler helpers

    #region Surface logic

    private Surface4 ClassifySurface(Vector2 normal)
    {
        Vector2 normalizedNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;
        if (normalizedNormal.y > +0.7071f) return Surface4.Floor;
        if (normalizedNormal.y < -0.7071f) return Surface4.Ceiling;
        return (normalizedNormal.x < 0f) ? Surface4.RightWall : Surface4.LeftWall;
    }

    private Vector2 SurfaceNormal(Surface4 s)
    {
        switch (s)
        {
            case Surface4.Floor: return Vector2.up;
            case Surface4.Ceiling: return Vector2.down;
            case Surface4.RightWall: return Vector2.left;
            case Surface4.LeftWall: return Vector2.right;
        }
        return Vector2.up;
    }

    private Vector2 StepForward(CrawlStep s)
    {
        switch (s.surface)
        {
            case Surface4.Floor: return Vector2.right * s.dir;
            case Surface4.Ceiling: return Vector2.right * s.dir;
            case Surface4.RightWall: return Vector2.up * s.dir;
            case Surface4.LeftWall: return Vector2.up * s.dir;
        }
        return Vector2.right;
    }

    private void SetStepImmediate(CrawlStep next)
    {
        step = next;

        AlignToNormal(SurfaceNormal(step.surface));
        EnsureFacingMatchesStep();
        ApplyStepCrawlSense();
    }

    private void EnsureFacingMatchesStep()
    {
        if (step.surface == Surface4.Floor)
        {
            bool wantRight = (step.dir > 0);
            if (enemy.IsFacingRight() != wantRight) enemy.Flip();
            return;
        }

        if (step.surface == Surface4.Ceiling)
        {
            bool wantRightOnCeiling = (step.dir < 0);
            if (enemy.IsFacingRight() != wantRightOnCeiling) enemy.Flip();
            return;
        }
    }

    private void ApplyStepCrawlSense()
    {
        switch (step.surface)
        {
            case Surface4.Floor: crawlSense = step.dir; break;
            case Surface4.Ceiling: crawlSense = -step.dir; break;
            case Surface4.RightWall: crawlSense = step.dir; break;
            case Surface4.LeftWall: crawlSense = -step.dir; break;
        }
    }

    #endregion

    #region Wall logic

    private CrawlStep AdvanceOnWallHit(CrawlStep s)
    {
        switch (s.surface)
        {
            case Surface4.Floor:
                return (s.dir > 0)
                    ? new CrawlStep(Surface4.LeftWall, +1)
                    : new CrawlStep(Surface4.RightWall, +1);

            case Surface4.Ceiling:
                return (s.dir > 0)
                    ? new CrawlStep(Surface4.LeftWall, -1)
                    : new CrawlStep(Surface4.RightWall, -1);

            case Surface4.LeftWall:
                return (s.dir > 0)
                    ? new CrawlStep(Surface4.Ceiling, -1)
                    : new CrawlStep(Surface4.Floor, -1);

            case Surface4.RightWall:
                return (s.dir > 0)
                    ? new CrawlStep(Surface4.Ceiling, +1)
                    : new CrawlStep(Surface4.Floor, +1);
        }

        return s;
    }

    private bool LocalWallProbe(out RaycastHit2D hit)
    {
        var wallCheck = enemy.GetWallCheck();
        if (wallCheck == null)
        {
            hit = default;
            return false;
        }

        Vector2 origin = wallCheck.position;
        Vector2 direction = StepForward(step).normalized;

        float distance = Mathf.Max(0.01f, enemy.GetWallCheckDistance());

        hit = Physics2D.Raycast(origin, direction, distance, enemy.GetWhatIsGround());
        return hit.collider != null;
    }

    #endregion

    #region Edge logic

    #region Edge probe / flow
    private bool LocalEdgeProbe()
    {
        if (edgeWrapActive) return false;

        Collider2D collider = enemy.GetComponent<Collider2D>();
        if (collider == null) return false;

        Bounds colliderBounds = collider.bounds;

        Vector2 forward = new Vector2(currentNormal.y, -currentNormal.x) * crawlSense;
        if (forward.sqrMagnitude < 0.0001f)
            forward = StepForward(step);
        forward = forward.normalized;

        Vector2 down;
        if (step.surface == Surface4.RightWall) down = Vector2.left;
        else if (step.surface == Surface4.LeftWall) down = Vector2.right;
        else down = -currentNormal.normalized;

        float ahead = Vector2.Dot(colliderBounds.extents, new Vector2(Mathf.Abs(forward.x), Mathf.Abs(forward.y)));
        ahead = Mathf.Max(0.05f, ahead);

        float downDistance = Vector2.Dot(colliderBounds.extents, new Vector2(Mathf.Abs(down.x), Mathf.Abs(down.y)));
        downDistance = Mathf.Max(0.10f, downDistance + EDGE_ATTACH_CAST_EXTRA);

        Vector2 origin = (Vector2)colliderBounds.center + forward * ahead;

        RaycastHit2D hit = Physics2D.Raycast(origin, down, downDistance, enemy.GetWhatIsGround());
        return hit.collider == null;
    }
    private void UpdateEdgeWrap()
    {
        switch (edgeWrapPhase)
        {
            case EdgeWrapPhase.RotateAtFeet:
                UpdateRotateAtFeetPhase();
                break;

            case EdgeWrapPhase.NudgeClear:
                UpdateNudgeClearPhase();
                break;

            case EdgeWrapPhase.OffsetToNextSurface:
                UpdateOffsetToNextSurfacePhase();
                break;

            case EdgeWrapPhase.Confirm:
                Wrap_Confirm();
                edgeWrapPhase = EdgeWrapPhase.None;
                break;

            default:
                ResetEdgeWrapState();
                break;
        }
    }
    private void DoEdgeOffsetWrap(CrawlStep from)
    {
        edgeWrapActive = true;
        edgeWrapPhase = EdgeWrapPhase.RotateAtFeet;
        rb.linearVelocity = Vector2.zero;

        edgeWrapCap = enemy.GetComponent<CapsuleCollider2D>();
        if (edgeWrapCap == null)
        {
            ResetEdgeWrapState();
            return;
        }

        Physics2D.SyncTransforms();

        edgeWrapFromStep = from;
        edgeWrapWidth = Mathf.Max(0.05f, edgeWrapCap.bounds.size.x);

        ResolveEdgeRule(from, out edgeNextStep, out edgeTargetZ, out _);

        Vector2 enemyDownWorld = -((Vector2)enemy.transform.up);
        Vector2 moveForwardWorld = StepForward(from).normalized;
        Vector2 feetDirWorld = (enemyDownWorld + moveForwardWorld).normalized;

        edgeWrapFeetLocal = CapsuleSupportPointLocal(edgeWrapCap, feetDirWorld);
        edgeWrapFeetWorldPinned = edgeWrapCap.transform.TransformPoint(edgeWrapFeetLocal);

        edgeRotateStartPos = rb.position;
        edgeRotateStartZ = Normalize360(enemy.transform.eulerAngles.z);

        SaveTransformState(out Vector2 savedPosition, out float savedZ);

        Wrap_RotateAtFeet(edgeWrapCap, edgeWrapFeetLocal, edgeWrapFeetWorldPinned);

        edgeRotateTargetPos = rb.position;
        edgeRotateTargetZ = Normalize360(edgeTargetZ);

        RestoreTransformState(savedPosition, savedZ);

        float speed = Mathf.Max(0.01f, enemy.moveSpeed * enemy.moveSpeedMultiplier);
        float distance = Vector2.Distance(edgeRotateStartPos, edgeRotateTargetPos);
        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(edgeRotateStartZ, edgeRotateTargetZ));

        float rotateSpeedMult = 2f;
        float rotateDuration = Mathf.Max(0.02f, distance / (speed * rotateSpeedMult));

        edgeRotateMoveSpeed = distance / rotateDuration;
        edgeRotateTurnSpeed = angleDelta / rotateDuration;
        edgeNudgeMoveSpeed = 0f;
    }

    #endregion

    #region Rotate phase
    private void UpdateRotateAtFeetPhase()
    {
        if (edgeWrapCap == null)
        {
            ResetEdgeWrapState();
            return;
        }

        rb.linearVelocity = Vector2.zero;

        float moveStep = edgeRotateMoveSpeed * Time.deltaTime;
        rb.position = Vector2.MoveTowards(rb.position, edgeRotateTargetPos, moveStep);

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = Normalize360(Mathf.MoveTowardsAngle(eulerAngles.z, edgeRotateTargetZ, edgeRotateTurnSpeed * Time.deltaTime));
        enemy.transform.eulerAngles = eulerAngles;
        Physics2D.SyncTransforms();

        bool posDone = ((Vector2)rb.position - edgeRotateTargetPos).sqrMagnitude <= 0.000001f;
        bool rotDone = Mathf.Abs(Mathf.DeltaAngle(enemy.transform.eulerAngles.z, edgeRotateTargetZ)) <= 0.1f;

        if (posDone && rotDone)
        {
            rb.position = edgeRotateTargetPos;

            eulerAngles = enemy.transform.eulerAngles;
            eulerAngles.z = Normalize360(edgeRotateTargetZ);
            enemy.transform.eulerAngles = eulerAngles;
            Physics2D.SyncTransforms();

            edgeWrapPhase = EdgeWrapPhase.NudgeClear;
        }
    }
    private void Wrap_RotateAtFeet(CapsuleCollider2D cap, Vector2 feetLocal, Vector2 feetWorldPinned)
    {
        Vector2 rbPosBefore = rb.position;
        float zBefore = enemy.transform.eulerAngles.z;

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = Normalize360(edgeTargetZ);
        enemy.transform.eulerAngles = eulerAngles;
        Physics2D.SyncTransforms();

        Vector2 feetWorldAfterRotate = cap.transform.TransformPoint(feetLocal);
        Vector2 delta = feetWorldPinned - feetWorldAfterRotate;

        rb.position = rb.position + delta;
        Physics2D.SyncTransforms();
    }
    #endregion

    #region Nudge phase
    private void UpdateNudgeClearPhase()
    {
        rb.linearVelocity = Vector2.zero;

        if (edgeNudgeMoveSpeed <= 0f)
        {
            float nudgeSpeedMult = 3.5f;
            edgeNudgeMoveSpeed = Mathf.Max(0.01f, enemy.moveSpeed * enemy.moveSpeedMultiplier * nudgeSpeedMult);
        }

        bool stillBlocked = IsNudgeProbeBlocked(edgeWrapFromStep, edgeWrapWidth);

        if (!stillBlocked)
        {
            if (edgeFreezeEnabled && edgeWrapFromStep.surface == edgeFreezeSurface)
            {
                edgeFreezeActive = true;
                return;
            }

            edgeOffsetStartPos = rb.position;

            SaveTransformState(out Vector2 savedPosition, out float savedZ);

            ApplyOffsetHalfStepOnNextSurface(edgeWrapFromStep, edgeWrapWidth);
            edgeOffsetTargetPos = rb.position;

            RestoreTransformState(savedPosition, savedZ);

            float offsetSpeed = Mathf.Max(0.01f, enemy.moveSpeed * enemy.moveSpeedMultiplier);
            float offsetDistance = Vector2.Distance(edgeOffsetStartPos, edgeOffsetTargetPos);
            float offsetDuration = Mathf.Max(0.02f, offsetDistance / offsetSpeed);
            edgeOffsetMoveSpeed = offsetDistance / offsetDuration;

            edgeWrapPhase = EdgeWrapPhase.OffsetToNextSurface;
            return;
        }

        Vector2 moveDir = StepForward(edgeWrapFromStep).normalized;
        if (moveDir.sqrMagnitude < 0.0001f)
            moveDir = GetEnemyWrapMoveDir();

        float moveStep = edgeNudgeMoveSpeed * Time.deltaTime;
        rb.position += moveDir * moveStep;
        Physics2D.SyncTransforms();
    }
    private bool IsNudgeProbeBlocked(CrawlStep from, float width)
    {
        var cap = enemy.GetComponent<CapsuleCollider2D>();
        if (cap == null)
            return false;

        Vector2 feetDir = -((Vector2)enemy.transform.up);
        if (feetDir.sqrMagnitude < 0.0001f)
            feetDir = Vector2.down;
        feetDir.Normalize();

        Vector2 moveDir = (Vector2)enemy.transform.right;
        if (moveDir.sqrMagnitude < 0.0001f)
            moveDir = GetEnemyWrapMoveDir();
        moveDir.Normalize();

        float skin = Mathf.Max(0.0005f, Physics2D.defaultContactOffset);
        float rayLength = width;

        Vector2 currentFeetLocal = CapsuleSupportPointLocal(cap, feetDir);
        Vector2 probeWorld = cap.transform.TransformPoint(currentFeetLocal);

        Vector2 rayOrigin = probeWorld + moveDir * skin;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, moveDir, rayLength, enemy.GetWhatIsGround());

        return hit.collider != null;
    }

    #endregion

    #region Offset phase
    private void UpdateOffsetToNextSurfacePhase()
    {
        rb.linearVelocity = Vector2.zero;

        float moveStep = edgeOffsetMoveSpeed * Time.deltaTime;
        rb.position = Vector2.MoveTowards(rb.position, edgeOffsetTargetPos, moveStep);
        Physics2D.SyncTransforms();

        bool posDone = ((Vector2)rb.position - edgeOffsetTargetPos).sqrMagnitude <= 0.000001f;
        if (!posDone)
            return;

        rb.position = edgeOffsetTargetPos;
        Physics2D.SyncTransforms();

        edgeWrapPhase = EdgeWrapPhase.Confirm;
    }
    private void ApplyOffsetHalfStepOnNextSurface(CrawlStep from, float width)
    {
        Vector2 offsetDirection = StepForward(edgeNextStep).normalized;
        rb.position = rb.position + offsetDirection * (width * 0.5f);
        Physics2D.SyncTransforms();
    }

    #endregion

    #region Confirm phase
    private void Wrap_Confirm()
    {
        SetStepImmediate(edgeNextStep);
        edgeWrapActive = false;
    }

    #endregion

    #region Edge helper methods
    private void SaveTransformState(out Vector2 position, out float zRotation)
    {
        position = rb.position;
        zRotation = enemy.transform.eulerAngles.z;
    }

    private void RestoreTransformState(Vector2 position, float zRotation)
    {
        rb.position = position;

        var eulerAngles = enemy.transform.eulerAngles;
        eulerAngles.z = zRotation;
        enemy.transform.eulerAngles = eulerAngles;

        Physics2D.SyncTransforms();
    }

    private Vector2 GetEnemyWrapMoveDir()
    {
        Vector2 direction = enemy.IsFacingRight()
            ? (Vector2)enemy.transform.right
            : -(Vector2)enemy.transform.right;

        if (direction.sqrMagnitude < 0.0001f)
            direction = StepForward(edgeWrapFromStep);

        return direction.normalized;
    }

    private void ResetEdgeWrapState()
    {
        edgeWrapActive = false;
        edgeWrapPhase = EdgeWrapPhase.None;
        edgeFreezeActive = false;
    }

    private static Vector2 CapsuleSupportPointLocal(CapsuleCollider2D cap, Vector2 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.0001f)
            worldDir = Vector2.down;

        worldDir.Normalize();

        Bounds colliderBounds = cap.bounds;
        Vector2 center = colliderBounds.center;

        float reach = Mathf.Max(colliderBounds.size.x, colliderBounds.size.y) * 2f;
        Vector2 farPoint = center + worldDir * reach;

        Vector2 supportWorld = cap.ClosestPoint(farPoint);

        return cap.transform.InverseTransformPoint(supportWorld);
    }

    private void ResolveEdgeRule(CrawlStep from, out CrawlStep next, out float targetZ, out float arcDeltaZ)
    {
        switch (from.surface)
        {
            case Surface4.Floor:
                if (from.dir > 0)
                {
                    next = new CrawlStep(Surface4.RightWall, -1);
                    targetZ = 270f;
                    arcDeltaZ = -90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.LeftWall, -1);
                    targetZ = 270f;
                    arcDeltaZ = +90f;
                }
                return;

            case Surface4.RightWall:
                if (from.dir < 0)
                {
                    next = new CrawlStep(Surface4.Ceiling, -1);
                    targetZ = 180f;
                    arcDeltaZ = +90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.Floor, -1);
                    targetZ = 0f;
                    arcDeltaZ = -90f;
                }
                return;

            case Surface4.Ceiling:
                if (from.dir > 0)
                {
                    next = new CrawlStep(Surface4.RightWall, +1);
                    targetZ = 90f;
                    arcDeltaZ = +90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.LeftWall, +1);
                    targetZ = 90f;
                    arcDeltaZ = -90f;
                }
                return;

            case Surface4.LeftWall:
                if (from.dir > 0)
                {
                    next = new CrawlStep(Surface4.Floor, +1);
                    targetZ = 0f;
                    arcDeltaZ = +90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.Ceiling, +1);
                    targetZ = 180f;
                    arcDeltaZ = -90f;
                }
                return;
        }

        next = from;
        targetZ = Vector2.SignedAngle(Vector2.up, currentNormal);
        arcDeltaZ = -90f;
    }

    #endregion

    #endregion

    #region Alignment logic

    private void SmoothAlignToCurrentNormal()
    {
        if (enemy.moveMode != RegularMoveMode.SurfaceCrawler) return;

        if (wallAlignActive)
        {
            SmoothAlignWall();
            return;
        }
    }

    private float ComputeWallTargetZ()
    {
        Vector2 wallNormalForRotation = currentNormal;

        if (step.surface == Surface4.RightWall)
            wallNormalForRotation = SurfaceNormal(Surface4.LeftWall);
        else if (step.surface == Surface4.LeftWall)
            wallNormalForRotation = SurfaceNormal(Surface4.RightWall);

        float targetZ = Vector2.SignedAngle(Vector2.up, wallNormalForRotation);

        bool onWall = step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall;
        if (onWall && !enemy.IsFacingRight())
            targetZ += 180f;

        return Normalize360(targetZ);
    }

    private void BeginWallAlign()
    {
        wallAlignActive = true;
        wallAlignTargetZ = ComputeWallTargetZ();

        zSmoothVel = 0f;
    }

    private void SmoothAlignWall()
    {
        var eulerAngles = enemy.transform.eulerAngles;

        float smoothTime = Mathf.Max(0.0001f, Z_ALIGN_TIME);
        eulerAngles.z = Mathf.SmoothDampAngle(eulerAngles.z, wallAlignTargetZ, ref zSmoothVel, smoothTime);
        enemy.transform.eulerAngles = eulerAngles;

        float remaining = Mathf.Abs(Mathf.DeltaAngle(eulerAngles.z, wallAlignTargetZ));
        if (remaining <= WALL_ALIGN_EPS_DEG)
        {
            eulerAngles.z = wallAlignTargetZ;
            enemy.transform.eulerAngles = eulerAngles;

            wallAlignActive = false;
            zSmoothVel = 0f;
        }
    }

    #endregion

    #endregion

    #region General helpers

    private void AlignToNormal(Vector2 normal)
    {
        currentNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;
    }

    private static float Normalize360(float a)
    {
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    private void TryRandomizeFacingOnEnter()
    {
        if (enemy.boundaryTouchedOnMove)
        {
            enemy.boundaryTouchedOnMove = false;
            return;
        }

        if (!enemy.randomizeFacingOnMoveEnter)
            return;

        if (Random.value >= 0.5f)
            return;

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            enemy.Flip();
            return;
        }

        bool clearWallBehind = !enemy.IsWallBehindDetected();
        bool hasGroundBehind = enemy.IsGroundBehindDetected();

        float nextXTowardBehind = enemy.transform.position.x - enemy.facingDir * 0.05f;
        bool staysInBounds = !IsOutsidePatrolX(nextXTowardBehind);

        if (clearWallBehind && hasGroundBehind && staysInBounds)
        {
            enemy.Flip();
        }
    }

    private bool PlayerDetectedValid()
    {
        if (enemy.IsPlayerDetected()) return true;

        var player = PlayerUtils.GetPlayerSafe();
        if (player == null) return false;

        return Vector2.Distance(enemy.transform.position, player.transform.position) < enemy.agroDistance;
    }

    #endregion
}