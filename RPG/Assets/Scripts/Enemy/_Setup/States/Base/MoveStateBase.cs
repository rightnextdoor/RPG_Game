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

    #endregion

    #region Edge wrap info
    private enum EdgeWrapPhase { ClearLip, Attach, Confirm }

    private bool edgeWrapActive;
    private EdgeWrapPhase edgeWrapPhase;

    private CrawlStep edgeFromStep;
    private CrawlStep edgeNextStep;

    private float edgeTargetZ;
    private float edgeZVel;

    private Vector2 edgeClearDir;
    private float edgeClearTimer;
    private float edgeHitIgnoreTimer;

    private bool edgeAttachHasTarget;
    private Vector2 edgeAttachTargetPos;
    private Vector2 edgeAttachNormal;

    private Bounds edgeAttachSurfaceBounds;
    private bool edgeAttachHasSurfaceBounds;

    private const float EDGE_ATTACH_MIN_OVERLAP = 0.70f;


    private Vector2 edgeArcPivot;
    private Vector2 edgeArcStartOffset;
    private float edgeArcT;
    private float edgeArcDeltaZ;


    private const float EDGE_CLEAR_TIME = 0.10f;
    private const float EDGE_ATTACH_CAST_EXTRA = 0.15f;
    private const float EDGE_Z_ALIGN_TIME = 0.05f;
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

            edgeWrapActive = false;
            edgeClearTimer = 0f;
            edgeZVel = 0f;
            edgeTargetZ = 0f;

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
            edgeClearTimer = 0f;
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
            SmoothAlignToCurrentNormal();
            UpdateEdgeWrap();
            return;
        }

        SmoothAlignToCurrentNormal();

        ApplyVelocityCrawler();

        if (edgeHitIgnoreTimer > 0f)
        {
            edgeHitIgnoreTimer -= Time.deltaTime;
            return;
        }

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
            BeginEdgeWrap(step);
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

    private float GetEdgeWrapStepDistance()
    {
        float speed = enemy.moveSpeed * enemy.moveSpeedMultiplier;
        return Mathf.Max(0.25f, speed) * Time.deltaTime;
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

        Vector2 forward = StepForward(step).normalized;
        Vector2 down = -currentNormal.normalized;

        float ahead = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(forward.x), Mathf.Abs(forward.y)));
        ahead = Mathf.Max(0.05f, ahead);

        float downDist = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(down.x), Mathf.Abs(down.y)));
        downDist = Mathf.Max(0.10f, downDist + EDGE_ATTACH_CAST_EXTRA);

        Vector2 origin = (Vector2)b.center + forward * ahead;

        RaycastHit2D hit = Physics2D.Raycast(origin, down, downDist, enemy.GetWhatIsGround());
        return hit.collider == null;
    }



    private void BeginEdgeWrap(CrawlStep from)
    {
        edgeWrapActive = true;
        edgeAttachHasTarget = false;
        edgeAttachHasSurfaceBounds = false;
        wallAlignActive = false;
        zSmoothVel = 0f;

        edgeWrapPhase = EdgeWrapPhase.ClearLip;

        edgeFromStep = from;
        ResolveEdgeRule(from, out edgeNextStep, out edgeTargetZ, out edgeArcDeltaZ);

        edgeArcT = 0f;
        edgeZVel = 0f;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;

            Vector2 forward = StepForward(from).normalized;
            Vector2 around = (-SurfaceNormal(from.surface)).normalized;

            float forwardExtent = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(forward.x), Mathf.Abs(forward.y)));
            float aroundExtent = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(around.x), Mathf.Abs(around.y)));

            forwardExtent = Mathf.Max(0.05f, forwardExtent);
            aroundExtent = Mathf.Max(0.05f, aroundExtent);

            edgeArcPivot = (Vector2)b.center + forward * forwardExtent + around * aroundExtent;
            edgeArcStartOffset = (Vector2)rb.position - edgeArcPivot;
        }
        else
        {
            edgeArcPivot = rb.position;
            edgeArcStartOffset = Vector2.zero;
        }

        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateEdgeWrap()
    {
        switch (edgeWrapPhase)
        {
            case EdgeWrapPhase.ClearLip:
                EdgeWrap_ClearLip();
                break;

            case EdgeWrapPhase.Attach:
                EdgeWrap_Attach();
                break;

            case EdgeWrapPhase.Confirm:
                EdgeWrap_Confirm();
                break;
        }
    }

    private void EdgeWrap_ClearLip()
    {
        float inv = (EDGE_CLEAR_TIME > 0.0001f) ? (1f / EDGE_CLEAR_TIME) : 1f;
        edgeArcT = Mathf.Clamp01(edgeArcT + Time.deltaTime * inv);

        float turnZ = edgeArcDeltaZ * edgeArcT;

        float rad = turnZ * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector2 o = edgeArcStartOffset;
        Vector2 rotated = new Vector2(
            o.x * cos - o.y * sin,
            o.x * sin + o.y * cos
        );

        rb.position = edgeArcPivot + rotated;

        if (edgeArcT >= 1f)
        {
            edgeWrapPhase = EdgeWrapPhase.Attach;
        }
    }

    private void EdgeWrap_Attach()
    {
        bool attached = TryAttachToNextSurface(edgeNextStep.surface);

        if (attached)
        {
            edgeWrapPhase = EdgeWrapPhase.Confirm;
            return;
        }

        Vector2 advance = StepForward(edgeNextStep).normalized;
        float step = GetEdgeWrapStepDistance();

        rb.position += advance * step;
    }


    private void EdgeWrap_Confirm()
    {
        SetStepImmediate(edgeNextStep);

        edgeWrapActive = false;

        edgeHitIgnoreTimer = .8f;
    }


    private bool IsAdheredToNormal(Vector2 normal)
    {
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null) return true;

        Bounds b = col.bounds;

        Vector2 down = -normal.normalized;
        float downDist = Vector2.Dot(b.extents, new Vector2(Mathf.Abs(down.x), Mathf.Abs(down.y)));
        downDist = Mathf.Max(0.08f, downDist + 0.05f);

        RaycastHit2D hit = Physics2D.Raycast((Vector2)b.center, down, downDist, enemy.GetWhatIsGround());
        return hit.collider != null;
    }

    private static float ComputeSurfaceOverlapPct(Bounds enemyBounds, Bounds surfaceBounds, Vector2 surfaceNormal)
    {
        bool tangentIsX = Mathf.Abs(surfaceNormal.y) > 0.5f;

        float enemyMin = tangentIsX ? enemyBounds.min.x : enemyBounds.min.y;
        float enemyMax = tangentIsX ? enemyBounds.max.x : enemyBounds.max.y;

        float surfMin = tangentIsX ? surfaceBounds.min.x : surfaceBounds.min.y;
        float surfMax = tangentIsX ? surfaceBounds.max.x : surfaceBounds.max.y;

        float overlap = Mathf.Min(enemyMax, surfMax) - Mathf.Max(enemyMin, surfMin);
        float enemySize = Mathf.Max(0.0001f, (tangentIsX ? enemyBounds.size.x : enemyBounds.size.y));

        return Mathf.Clamp01(overlap / enemySize);
    }


    private bool TryAttachToNextSurface(Surface4 nextSurface)
    {
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null)
            return true;

        if (edgeAttachHasTarget)
            return ConvergeEdgeAttachTarget(col);

        return AcquireEdgeAttachTarget(col, nextSurface);
    }

    private bool ConvergeEdgeAttachTarget(Collider2D col)
    {
        float step = GetEdgeWrapStepDistance();

        rb.position = Vector2.MoveTowards(rb.position, edgeAttachTargetPos, step);
        AlignToNormal(edgeAttachNormal);

        bool adhered = IsAdheredToNormal(edgeAttachNormal) || IsAdheredToNormal(-edgeAttachNormal);

        bool overlappedEnough = false;
        if (edgeAttachHasSurfaceBounds)
        {
            Bounds enemyB = col.bounds;
            float overlapPct = ComputeSurfaceOverlapPct(enemyB, edgeAttachSurfaceBounds, edgeAttachNormal);
            overlappedEnough = overlapPct >= EDGE_ATTACH_MIN_OVERLAP;
        }

        if (adhered && overlappedEnough)
        {
            edgeAttachHasTarget = false;
            edgeAttachHasSurfaceBounds = false;
            return true;
        }

        return false;
    }

    private bool AcquireEdgeAttachTarget(Collider2D col, Surface4 nextSurface)
    {
        Bounds b = col.bounds;

        Vector2 desiredNormal = SurfaceNormal(nextSurface).normalized;
        Vector2 castDir = -desiredNormal;

        float baseDist = Vector2.Dot(
            b.extents,
            new Vector2(Mathf.Abs(desiredNormal.x), Mathf.Abs(desiredNormal.y))
        );
        baseDist = Mathf.Max(0.10f, baseDist);

        float castDist = Mathf.Max(0.15f, baseDist + EDGE_ATTACH_CAST_EXTRA);
        Vector2 origin = (Vector2)b.center + desiredNormal * castDist;
        float castLen = castDist * 2f;

        RaycastHit2D hit = Physics2D.Raycast(origin, castDir, castLen, enemy.GetWhatIsGround());
        if (hit.collider == null)
            return false;

        const float SKIN = 0.02f;

        edgeAttachTargetPos = hit.point + desiredNormal * (baseDist + SKIN);
        edgeAttachNormal = desiredNormal;

        edgeAttachSurfaceBounds = hit.collider.bounds;
        edgeAttachHasSurfaceBounds = true;
        edgeAttachHasTarget = true;

        float stepNow = GetEdgeWrapStepDistance();
        rb.position = Vector2.MoveTowards(rb.position, edgeAttachTargetPos, stepNow);
        AlignToNormal(edgeAttachNormal);

        return false;
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
                    next = new CrawlStep(Surface4.Ceiling, +1);
                    targetZ = 180f;
                    arcDeltaZ = +90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.Floor, +1);
                    targetZ = 0f;
                    arcDeltaZ = -90f;
                }
                return;

            case Surface4.Ceiling:
                if (from.dir > 0)
                {
                    next = new CrawlStep(Surface4.RightWall, +1);
                    targetZ = 90f;
                    arcDeltaZ = -90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.LeftWall, +1);
                    targetZ = 90f;
                    arcDeltaZ = +90f;
                }
                return;

            case Surface4.LeftWall:
                if (from.dir > 0)
                {
                    next = new CrawlStep(Surface4.Floor, -1);
                    targetZ = 0f;
                    arcDeltaZ = +90f;
                }
                else
                {
                    next = new CrawlStep(Surface4.Ceiling, -1);
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

        if (edgeWrapActive)
        {
            SmoothAlignEdge();
            return;
        }

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
        //float targetZ = Vector2.SignedAngle(Vector2.up, currentNormal);

        //bool onWall = step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall;
        //if (onWall && !enemy.IsFacingRight()) targetZ += 180f;

        //var e = enemy.transform.eulerAngles;
        //float smoothTime = Mathf.Max(0.0001f, Z_ALIGN_TIME);
        //e.z = Mathf.SmoothDampAngle(e.z, Normalize360(targetZ), ref zSmoothVel, smoothTime);
        //enemy.transform.eulerAngles = e;

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

    private void SmoothAlignEdge()
    {
        var e = enemy.transform.eulerAngles;
        float smoothTime = Mathf.Max(0.0001f, EDGE_Z_ALIGN_TIME);
        e.z = Mathf.SmoothDampAngle(e.z, Normalize360(edgeTargetZ), ref edgeZVel, smoothTime);
        enemy.transform.eulerAngles = e;
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
