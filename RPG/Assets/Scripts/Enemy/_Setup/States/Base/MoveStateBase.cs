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
            edgeWrapActive = false;

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
            float a = Mathf.Max(0f, enemy.traverseStopMinSeconds);
            float b = Mathf.Max(a, enemy.traverseStopMaxSeconds);
            traverseStopTimer = (b > 0f) ? Random.Range(a, b) : 0f;
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
            edgeWrapActive = false;
        }
    }

    #region Modes Methods

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
            OnBoundary();
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
        if (edgeWrapActive)
        {
            return;
        }

        SmoothAlignToCurrentNormal();
        ApplyVelocityCrawler();

        bool wallHit = LocalWallProbe(out var _);
        if (wallHit)
        {
            var next = AdvanceOnWallHit(step);
            SetStepImmediate(next);
            BeginWallAlign();
            return;
        }

        if (wallAlignActive)
            return;

        bool edgeHit = LocalEdgeProbe();
        if (edgeHit)
        {

            DoEdgeOffsetWrap(step);          
            return;
        }
    }


    #endregion

    #region Patrol Boundary
    private bool ReturnToPatrolCenterIfOutside()
    {
        float x = enemy.transform.position.x;
        if (!IsOutsidePatrolX(x))
            return false;

        int desiredDir = (enemy.patrolCenter.x - x) >= 0f ? +1 : -1;
        if (enemy.facingDir != desiredDir)
            enemy.Flip();

        ApplyVelocity();

        const float centerSnapEpsilon = 0.1f;
        return Mathf.Abs(enemy.patrolCenter.x - x) > centerSnapEpsilon;
    }

    private bool IsBoundaryAheadInFacingDir()
    {
        float x = enemy.transform.position.x;

        return (enemy.facingDir > 0 && x >= enemy.patrolRightX)
            || (enemy.facingDir < 0 && x <= enemy.patrolLeftX);
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

        float vx = enemy.moveSpeed * enemy.moveSpeedMultiplier * enemy.facingDir;
        enemy.SetVelocity(vx, rb.linearVelocity.y);
    }

    private void ApplyVelocityCrawler()
    {
        float speed = enemy.moveSpeed * enemy.moveSpeedMultiplier;

        Vector2 tangent = new Vector2(currentNormal.y, -currentNormal.x) * crawlSense;
        Vector2 targetVel = tangent.normalized * speed;

        float alpha = 1f - Mathf.Exp(-VEL_BLEND_HZ * Time.deltaTime);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, alpha);
    }

    #endregion

    #region Surface Crawler Helpers

    #region Suface logic

    private Surface4 ClassifySurface(Vector2 n)
    {
        Vector2 nn = (n.sqrMagnitude > 0.0001f) ? n.normalized : Vector2.up;
        if (nn.y > +0.7071f) return Surface4.Floor;
        if (nn.y < -0.7071f) return Surface4.Ceiling;
        return (nn.x < 0f) ? Surface4.RightWall : Surface4.LeftWall;
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
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, +1)
                                   : new CrawlStep(Surface4.LeftWall, +1);
            case Surface4.Ceiling:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, -1)
                                   : new CrawlStep(Surface4.LeftWall, -1);

            case Surface4.RightWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Ceiling, -1)
                                   : new CrawlStep(Surface4.Floor, -1);

            case Surface4.LeftWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Ceiling, +1)
                                   : new CrawlStep(Surface4.Floor, +1);
        }
        return s;
    }

    private bool LocalWallProbe(out RaycastHit2D hit)
    {
        var wallT = enemy.GetWallCheck();
        if (wallT == null) { hit = default; return false; }

        Vector2 origin = (Vector2)wallT.position;
        Vector2 dir = StepForward(step);
        float dist = Mathf.Max(0.01f, enemy.GetWallCheckDistance());

        hit = Physics2D.Raycast(origin, dir, dist, enemy.GetWhatIsGround());
        return hit.collider != null;
    }
    #endregion

    #region Edge logic

    private bool LocalEdgeProbe()
    {
        if (edgeWrapActive) return false;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null) return false;

        Bounds b = col.bounds;

        Vector2 forward = new Vector2(currentNormal.y, -currentNormal.x) * crawlSense;
        if (forward.sqrMagnitude < 0.0001f)
            forward = StepForward(step);
        forward = forward.normalized;

        Vector2 down;
        if (step.surface == Surface4.RightWall) down = Vector2.left;
        else if (step.surface == Surface4.LeftWall) down = Vector2.right;
        else down = -currentNormal.normalized;

        float ahead = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(forward.x), Mathf.Abs(forward.y)));
        ahead = Mathf.Max(0.05f, ahead);

        float downDist = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(down.x), Mathf.Abs(down.y)));
        downDist = Mathf.Max(0.10f, downDist + EDGE_ATTACH_CAST_EXTRA);

        Vector2 origin = (Vector2)b.center + forward * ahead;

        RaycastHit2D hit = Physics2D.Raycast(origin, down, downDist, enemy.GetWhatIsGround());
        return hit.collider == null;
    }

    private void DoEdgeOffsetWrap(CrawlStep from)
    {
        edgeWrapActive = true;
        rb.linearVelocity = Vector2.zero;

        var cap = enemy.GetComponent<CapsuleCollider2D>();
        if (cap == null)
        {
            edgeWrapActive = false;
            return;
        }

        Physics2D.SyncTransforms();

        float width = Mathf.Max(0.05f, cap.bounds.size.x);

        ResolveEdgeRule(from, out edgeNextStep, out edgeTargetZ, out _);

        Vector2 enemyDownWorld = -((Vector2)enemy.transform.up);
        Vector2 moveForwardWorld = StepForward(from).normalized;
        Vector2 feetDirWorld = (enemyDownWorld + moveForwardWorld).normalized;

        Vector2 feetLocal = CapsuleSupportPointLocal(cap, feetDirWorld);
        Vector2 feetWorldPinned = cap.transform.TransformPoint(feetLocal);

        Wrap_RotateAtFeet(cap, feetLocal, feetWorldPinned);

        Wrap_NudgeAndOffset(from, width, feetLocal);

        Wrap_Confirm();
    }

    private void Wrap_RotateAtFeet(CapsuleCollider2D cap, Vector2 feetLocal, Vector2 feetWorldPinned)
    {
        Vector2 rbPosBefore = rb.position;
        float zBefore = enemy.transform.eulerAngles.z;

        var e = enemy.transform.eulerAngles;
        e.z = Normalize360(edgeTargetZ);
        enemy.transform.eulerAngles = e;
        Physics2D.SyncTransforms();

        Vector2 feetWorldAfterRotate = cap.transform.TransformPoint(feetLocal);
        Vector2 delta = feetWorldPinned - feetWorldAfterRotate;

        rb.position = rb.position + delta;
        Physics2D.SyncTransforms();

    }

    private void Wrap_NudgeAndOffset(CrawlStep from, float width, Vector2 feetLocal)
    {
        Vector2 nudgeDir = StepForward(from).normalized;

        Vector2 intoOldSurface = -((Vector2)enemy.transform.up);

        float skin = Mathf.Max(0.0005f, Physics2D.defaultContactOffset);
        float rayLen = width;

        var cap = enemy.GetComponent<CapsuleCollider2D>();
        if (cap == null)
            return;

        Vector2 probeWorld = cap.transform.TransformPoint(feetLocal);

        const int maxNudges = 24;
        int nudges = 0;

        while (nudges < maxNudges)
        {
            Vector2 rayOrigin = probeWorld - intoOldSurface * skin;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, intoOldSurface, rayLen, enemy.GetWhatIsGround());
            if (hit.collider == null)
                break;

            rb.position = rb.position + nudgeDir * skin;
            Physics2D.SyncTransforms();

            probeWorld = cap.transform.TransformPoint(feetLocal);

            nudges++;
        }

        Vector2 dir2 = StepForward(edgeNextStep).normalized;
        rb.position = rb.position + dir2 * (width * 0.5f);
        Physics2D.SyncTransforms();
    }

    private void Wrap_Confirm()
    {
        SetStepImmediate(edgeNextStep);
        edgeWrapActive = false;
    }

    private static Vector2 CapsuleSupportPointLocal(CapsuleCollider2D cap, Vector2 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.0001f)
            worldDir = Vector2.down;

        worldDir.Normalize();

        Bounds b = cap.bounds;
        Vector2 center = b.center;

        float reach = Mathf.Max(b.size.x, b.size.y) * 2f;
        Vector2 far = center + worldDir * reach;

        Vector2 supportWorld = cap.ClosestPoint(far);

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

    #region Smooth logic

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
        float targetZ = Vector2.SignedAngle(Vector2.up, currentNormal);

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
        var e = enemy.transform.eulerAngles;

        float smoothTime = Mathf.Max(0.0001f, Z_ALIGN_TIME);
        e.z = Mathf.SmoothDampAngle(e.z, wallAlignTargetZ, ref zSmoothVel, smoothTime);
        enemy.transform.eulerAngles = e;

        float remaining = Mathf.Abs(Mathf.DeltaAngle(e.z, wallAlignTargetZ));
        if (remaining <= WALL_ALIGN_EPS_DEG)
        {
            e.z = wallAlignTargetZ;
            enemy.transform.eulerAngles = e;

            wallAlignActive = false;
            zSmoothVel = 0f;
        }
    }

    #endregion

    #endregion

    #region Helper Methods
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
