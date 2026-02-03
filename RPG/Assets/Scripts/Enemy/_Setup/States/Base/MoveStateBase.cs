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
    private Surface4 pendingSurface;
    private int pendingDir;

    private enum EdgeWrapPhase { ClearLip, EdgeClear, Attach, Confirm }
    private EdgeWrapPhase wrapPhase = EdgeWrapPhase.ClearLip;

    private Vector2 currentNormal = Vector2.up;
    private float savedGravity;
    private float wrapRemaining;

    // --- Edge tuning
    private const float EDGE_CLEAR_FACTOR = 0.12f;
    private const float EDGE_CLEAR_PADDING = 0f;
    private const float EDGE_ROTATE_EPSILON = 2f;
    private const float WALL_EDGE_CLEAR_MULT = 1f;
    private const float SKIN_NUDGE = 0.02f;
    private const float POST_CONFIRM_DEFER_FRAMES = .5f;
    private bool suspendChecksUntilLock = false;
    private float postConfirmDeferFrames = 0;
    private int crawlSense = +1;

    private float zSmoothVel;
    private const float Z_ALIGN_TIME = 0.06f;
    private const float VEL_BLEND_HZ = 12f;
    #endregion

    // ---- DEBUG helpers for Surface Crawler ----
    private bool didLogEdgeRotate;
    private bool didLogEdgeRotateOverride;
    private string SurfaceToString(Surface4 s)
    {
        switch (s)
        {
            case Surface4.Floor: return "Floor";
            case Surface4.Ceiling: return "Ceiling";
            case Surface4.RightWall: return "Right Wall";
            case Surface4.LeftWall: return "Left Wall";
        }
        return "Unknown";
    }

    private string StepDirToString(CrawlStep s)
    {
        switch (s.surface)
        {
            case Surface4.Floor: return (s.dir > 0) ? "Right" : "Left";
            case Surface4.Ceiling: return (s.dir > 0) ? "Right" : "Left";
            case Surface4.RightWall: return (s.dir > 0) ? "Up" : "Down";
            case Surface4.LeftWall: return (s.dir > 0) ? "Up" : "Down";
        }
        return "?";
    }

    private string FeetForSurface(Surface4 s)
    {
        // "Feet must point into the surface"
        switch (s)
        {
            case Surface4.Floor: return "Down";
            case Surface4.Ceiling: return "Up";
            case Surface4.RightWall: return "Left";
            case Surface4.LeftWall: return "Right";
        }
        return "?";
    }

    private void LogCrawlerState(string prefix)
    {
        Debug.Log($"[Crawler] {enemy.name}: {prefix} | on {SurfaceToString(step.surface)} moving {StepDirToString(step)}");
    }

    private void LogCrawlerTransition(string eventType, CrawlStep from, CrawlStep to)
    {
        Debug.Log(
            $"[Crawler] {enemy.name}: {eventType} -> " +
            $"FROM [{SurfaceToString(from.surface)} {StepDirToString(from)}] " +
            $"TO   [{SurfaceToString(to.surface)} {StepDirToString(to)}]"
        );
    }

    // --- Single-fire log flags for this wrap
    private bool didLogEdgeHit, didLogAttach;

    // --- Attach pose / cooldown flags
    private bool inAttachPose = false;

    // --- Cache main collider for width projection
    private Collider2D mainCol;

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
            if (mainCol == null) mainCol = enemy.GetComponent<Collider2D>();

            needsPatrolReturn = false;
            didLogEdgeRotate = didLogEdgeRotateOverride = false;
            savedGravity = rb.gravityScale;
            rb.gravityScale = 0f;

            currentNormal = enemy.transform.up;

            TryRandomizeFacingOnEnter();

            if (LocalAdhesionProbe(out var hit))
                AlignToSurface(hit);
            else
                AlignToNormal(currentNormal);

            step = new CrawlStep(ClassifySurface(currentNormal), (enemy.facingDir >= 0) ? +1 : -1);
            ApplyStepCrawlSense();

            EnsureFacingMatchesStep();

            LogCrawlerState("Enter");

            wrapPhase = EdgeWrapPhase.ClearLip;
            wrapRemaining = 0f;
            postConfirmDeferFrames = 0;

            inAttachPose = false;
            suspendChecksUntilLock = false;
            didLogEdgeHit = didLogAttach = false;
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

            wrapPhase = EdgeWrapPhase.ClearLip;
            wrapRemaining = 0f;
            postConfirmDeferFrames = 0;

            inAttachPose = false;
            suspendChecksUntilLock = false;
            didLogEdgeHit = didLogAttach = false;
            didLogEdgeRotate = didLogEdgeRotateOverride = false;
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
        SmoothAlignToCurrentNormal();

        float speed = enemy.moveSpeed * enemy.moveSpeedMultiplier;

        if (TickEdgeWrap(speed))
            return;

        if (suspendChecksUntilLock)
        {
            if (postConfirmDeferFrames > 0f)
            {
                postConfirmDeferFrames -= Time.deltaTime;
                ApplyVelocityCrawler();
                return;
            }

            if (postConfirmDeferFrames <= 0f)
            {
                bool confirmHit = LocalAdhesionProbeConfirm(out var _);
                if (confirmHit)
                {
                    suspendChecksUntilLock = false;
                    Debug.Log($"[Crawler] {enemy.name}: CHECKS-RESUME wrap={wrapPhase} confirmHit=True");
                }
                else
                {
                    // stay suspended one more frame; keep gliding along the new surface
                    Debug.Log($"[Crawler] {enemy.name}: CHECKS-RESUME WAIT wrap={wrapPhase} confirmHit=False");
                    ApplyVelocityCrawler();
                    return;
                }

                ApplyVelocityCrawler();
                return;
            }
        }

        ApplyVelocityCrawler();

        bool edgeCheck = LocalAdhesionProbe(out var hitDown);
        Debug.Log($"[Crawler] {enemy.name}: edgeCheck={edgeCheck} phase={wrapPhase} suspend={suspendChecksUntilLock} defer={postConfirmDeferFrames:F3} surface={SurfaceToString(step.surface)} dir={StepDirToString(step)}");

        bool wallHit = edgeCheck && LocalWallProbe(out var _);

        if (wallHit)
        {
            var next = AdvanceOnWallHit(step);
            SetStepImmediate(next);
            return;
        }

        if (!edgeCheck)
        {
            if (LocalAdhesionProbeConfirm(out var _)) { ApplyVelocityCrawler(); return; }

            var next = AdvanceOnEdge(step);
            BeginEdgeWrap(next);

            if (TickEdgeWrap(speed)) return;
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

    private float ComputeColliderWidthAlong(Vector2 tangent)
    {
        if (mainCol == null) return Mathf.Max(0.01f, enemy.GetWallCheckDistance());
        Vector2 t = tangent.normalized;
        Bounds b = mainCol.bounds;
        // Project AABB size onto the tangent (support-mapped)
        float proj =
            Mathf.Abs(Vector2.Dot(t, Vector2.right)) * b.size.x +
            Mathf.Abs(Vector2.Dot(t, Vector2.up)) * b.size.y;
        return Mathf.Max(0.01f, proj);
    }

    private float ComputeColliderExtentAlong(Vector2 axis)
    {
        if (mainCol == null) return Mathf.Max(0.01f, enemy.GetGroundCheckDistance());
        Vector2 a = axis.normalized;
        Bounds b = mainCol.bounds;
        float proj =
            Mathf.Abs(Vector2.Dot(a, Vector2.right)) * b.size.x +
            Mathf.Abs(Vector2.Dot(a, Vector2.up)) * b.size.y;
        return Mathf.Max(0.01f, proj * 0.5f); // half-extent
    }

    private void BeginEdgeWrap(CrawlStep target)
    {
        pendingSurface = target.surface;
        pendingDir = target.dir;

        // UNIFORM clear distance across Floor/Wall/Ceiling
        float cornerSpan = ComputeColliderCornerClearance();
        wrapRemaining = cornerSpan * EDGE_CLEAR_FACTOR + EDGE_CLEAR_PADDING;

        if (step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall)
            wrapRemaining *= WALL_EDGE_CLEAR_MULT;

        wrapPhase = EdgeWrapPhase.EdgeClear;
        inAttachPose = false;
        didLogAttach = false;
        suspendChecksUntilLock = true;
        // leave postConfirmDeferFrames untouched here (only set on CONFIRM)
        didLogEdgeRotate = didLogEdgeRotateOverride = false;
    }

    private float ComputeColliderCornerClearance()
    {
        if (mainCol == null) return Mathf.Max(0.01f, enemy.GetGroundCheckDistance());
        Bounds b = mainCol.bounds;
        float halfDiag = 0.5f * Mathf.Sqrt(b.size.x * b.size.x + b.size.y * b.size.y);
        return Mathf.Max(0.01f, 2f * halfDiag);
    }

    private bool TickEdgeWrap(float speed)
    {
        switch (wrapPhase)
        {
            case EdgeWrapPhase.EdgeClear:
                {
                    // Move along the CURRENT surface just enough to clear the lip — no velocity drops.
                    Vector2 curTangent = StepForward(step);
                    Vector2 pendingTangent = StepForward(new CrawlStep(pendingSurface, pendingDir));
                    float stepThis = Mathf.Min(wrapRemaining, speed * Time.deltaTime);

                    // Anticipatory blend: start steering velocity toward the NEXT surface
                    // in the last few centimeters of the clear so there’s no “rotate → pause → move”.
                    // (no new constants; small inline window that feels natural)
                    float anticipatoryWindow = 0.06f;                 // ~6cm world space
                    float t = 1f - Mathf.Clamp01(wrapRemaining / Mathf.Max(anticipatoryWindow, 0.0001f));

                    Vector2 blendedTangent = Vector2.Lerp(curTangent, pendingTangent, t).normalized;
                    Vector2 targetVel = blendedTangent * speed;
                    float alpha = 1f - Mathf.Exp(-VEL_BLEND_HZ * Time.deltaTime);
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, alpha);

                    // Position still advances along the CURRENT surface so we truly clear the lip.
                    enemy.transform.position += (Vector3)(curTangent * stepThis);
                    wrapRemaining -= stepThis;

                    if (wrapRemaining > 0f) return true;

                    // Tiny lateral nudge only for walls so we've fully cleared the lip
                    if (step.surface == Surface4.RightWall || step.surface == Surface4.LeftWall)
                    {
                        Vector2 awayFromWall = -SurfaceNormal(step.surface);
                        enemy.transform.position += (Vector3)(awayFromWall * (SKIN_NUDGE + 0.01f));
                    }

                    // Enter ATTACH pose: feet aim into the pending surface; checks remain OFF
                    inAttachPose = true;
                    wrapPhase = EdgeWrapPhase.Attach;
                    return true;
                }

            case EdgeWrapPhase.Attach:
                {
                    // Glide ALONG the PENDING surface while rotation progresses.
                    Vector2 pendingTangent = StepForward(new CrawlStep(pendingSurface, pendingDir));
                    Vector2 targetVel = pendingTangent.normalized * speed;
                    float alpha = 1f - Mathf.Exp(-VEL_BLEND_HZ * Time.deltaTime);
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, alpha);

                    // Lock when adhesion ray INTO the pending surface hits.
                    if (LocalAdhesionProbeAgainst(pendingSurface, out var hitDown))
                    {
                        AlignToSurface(hitDown);

                        // Small inward bias to avoid hovering at threshold
                        enemy.transform.position += (Vector3)((-hitDown.normal).normalized * SKIN_NUDGE);

                        // Remove any outward-normal velocity so we don't bounce off the corner
                        {
                            Vector2 n = hitDown.normal.normalized;
                            Vector2 v = rb.linearVelocity;
                            float vN = Vector2.Dot(v, n);
                            if (vN > 0f) v -= vN * n;
                            rb.linearVelocity = v;
                        }

                        // Commit the new step (now "step" is the pending surface+dir)
                        var to = new CrawlStep(pendingSurface, pendingDir);
                        SetStepImmediate(to);

                        inAttachPose = false;
                        wrapPhase = EdgeWrapPhase.Confirm;
                        suspendChecksUntilLock = true;
                        {
                            Vector2 tan = StepForward(step).normalized; // uses the now-committed step
                            float vT = Vector2.Dot(rb.linearVelocity, tan);
                            if (vT < speed * 0.98f) rb.linearVelocity = tan * speed;
                        }
                    }
                    return true;
                }

            case EdgeWrapPhase.Confirm:
                {
                    // While rotation finishes, keep moving along the NEW surface smoothly.
                    Vector2 tangentNow = StepForward(step);
                    Vector2 targetVel = tangentNow.normalized * speed;
                    float alpha = 1f - Mathf.Exp(-VEL_BLEND_HZ * Time.deltaTime);
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, alpha);

                    // Don’t confirm until the attach rotation has settled.
                    if (!IsEdgeRotationSettled())
                        return true;

                    // Single confirm ray INTO the current (new) surface.
                    if (LocalAdhesionProbeConfirm(out var _))
                    {
                        // Finish wrap and start your short defer window
                        wrapPhase = EdgeWrapPhase.ClearLip;
                        suspendChecksUntilLock = true;
                        postConfirmDeferFrames = POST_CONFIRM_DEFER_FRAMES;

                        {
                            Vector2 tan = StepForward(step).normalized;
                            float vT = Vector2.Dot(rb.linearVelocity, tan);
                            if (vT < speed * 0.98f) rb.linearVelocity = tan * speed;
                        }
                        return false; // allow SurfaceCrawler() to continue this frame (no pause)
                    }

                    return true;
                }
        }
        return false;
    }


    private Surface4 ClassifySurface(Vector2 n)
    {
        Vector2 nn = (n.sqrMagnitude > 0.0001f) ? n.normalized : Vector2.up;
        if (nn.y > +0.7071f) return Surface4.Floor;
        if (nn.y < -0.7071f) return Surface4.Ceiling;
        return (nn.x < 0f) ? Surface4.RightWall : Surface4.LeftWall; // outward normal left→right wall
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

    private void EnsureFacingFor(Surface4 surface, int dir)
    {
        if (surface == Surface4.Floor)
        {
            bool wantRight = (dir > 0);
            if (enemy.IsFacingRight() != wantRight) enemy.Flip();
            return;
        }

        if (surface == Surface4.Ceiling)
        {
            bool wantRightOnCeiling = (dir < 0);
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

    private CrawlStep AdvanceOnWallHit(CrawlStep s)
    {
        switch (s.surface)
        {
            // Floor → Wall (climb UP)
            case Surface4.Floor:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, +1)   // Floor Right  → RightWall Up
                                   : new CrawlStep(Surface4.LeftWall, +1);  // Floor Left   → LeftWall  Up

            // Ceiling → Wall (climb DOWN)
            case Surface4.Ceiling:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, -1)   // Ceiling Right → RightWall Down
                                   : new CrawlStep(Surface4.LeftWall, -1);  // Ceiling Left  → LeftWall  Down

            // Wall → Cap (floor/ceiling) by vertical
            case Surface4.RightWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Ceiling, -1)     // RightWall Up   → Ceiling Left
                                   : new CrawlStep(Surface4.Floor, -1);     // RightWall Down → Floor   Left

            case Surface4.LeftWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Ceiling, +1)     // LeftWall Up    → Ceiling Right
                                   : new CrawlStep(Surface4.Floor, +1);     // LeftWall Down  → Floor   Right
        }
        return s;
    }

    // EDGE (GROUND) RULES (adhesion lost)
    private CrawlStep AdvanceOnEdge(CrawlStep s)
    {
        switch (s.surface)
        {
            case Surface4.Floor:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, -1)
                                   : new CrawlStep(Surface4.LeftWall, -1);

            case Surface4.Ceiling:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, +1)
                                   : new CrawlStep(Surface4.LeftWall, +1);

            case Surface4.RightWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Floor, -1)
                                   : new CrawlStep(Surface4.Ceiling, -1);

            case Surface4.LeftWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Floor, +1)
                                   : new CrawlStep(Surface4.Ceiling, +1);
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

    private void AlignToSurface(RaycastHit2D hit)
    {
        AlignToNormal((hit.normal.sqrMagnitude > 0.0001f) ? hit.normal.normalized : Vector2.up);
    }

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

    private bool IsEdgeRotationSettled()
    {
        Surface4 s = inAttachPose ? pendingSurface : step.surface;

        float targetZ = EdgeAttachTargetZForSurface(s);
        float actualZ = Normalize360(enemy.transform.eulerAngles.z);
        float delta = Mathf.Abs(Mathf.DeltaAngle(actualZ, targetZ));

        return delta <= EDGE_ROTATE_EPSILON && Mathf.Abs(zSmoothVel) < 0.01f;
    }


    private void SmoothAlignToCurrentNormal()
    {
        if (enemy.moveMode != RegularMoveMode.SurfaceCrawler) return;

        if (suspendChecksUntilLock)
            SmoothAlignEdge();
        else
            SmoothAlignWall();
    }

    private void SmoothAlignWall()
    {
        // Base target Z from the desired surface normal
        float targetZ = Vector2.SignedAngle(Vector2.up, currentNormal);

        // On walls and NOT facing right, rotate feet-side by 180°
        bool onWall = step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall;
        if (onWall && !enemy.IsFacingRight()) targetZ += 180f;

        // SmoothDampAngle to the target Z
        var e = enemy.transform.eulerAngles;
        float smoothTime = Mathf.Max(0.0001f, Z_ALIGN_TIME);
        e.z = Mathf.SmoothDampAngle(e.z, Normalize360(targetZ), ref zSmoothVel, smoothTime);
        enemy.transform.eulerAngles = e;
    }


    // ADD: EDGE logic (feet point INTO pending/current edge surface, with feet-pivot)
    private void SmoothAlignEdge()
    {
        // Target Z from the edge attach table
        Surface4 s = inAttachPose ? pendingSurface : step.surface;
        float targetZ = EdgeAttachTargetZForSurface(s);

        // Smooth toward attach angle
        var e = enemy.transform.eulerAngles;
        float smoothTime = Mathf.Max(0.0001f, Z_ALIGN_TIME);
        float prevZ = Normalize360(e.z);
        float nextZ = Mathf.SmoothDampAngle(prevZ, Normalize360(targetZ), ref zSmoothVel, smoothTime);
        float dZ = Mathf.DeltaAngle(prevZ, nextZ);

        e.z = Normalize360(prevZ + dZ);
        enemy.transform.eulerAngles = e;

        // Feet-pivot so the body doesn’t clip while rotating (edge only)
        var groundT = enemy.GetGroundCheck();
        if (groundT != null)
        {
            Vector3 pivot = groundT.position;
            Vector3 off = enemy.transform.position - pivot;
            enemy.transform.position = pivot + (Quaternion.Euler(0f, 0f, dZ) * off);
        }

        // Keep existing one-shot edge rotate log (edge only)
        if (!didLogEdgeRotate)
        {
            float actualZ = Normalize360(enemy.transform.eulerAngles.z);
            string phase = wrapPhase.ToString();
            string src = inAttachPose ? "AttachFeet(pending)" : "EdgeFeet(current)";
            string facing = enemy.IsFacingRight() ? "Right" : "Left";
            Debug.Log($"[Crawler] {enemy.name}: EDGE-ROTATE | phase={phase} src={src} " +
                      $"targetZ={Normalize360(targetZ):F1} actualZ={actualZ:F1} facing={facing} " +
                      $"pending={SurfaceToString(pendingSurface)} step=[{SurfaceToString(step.surface)} {StepDirToString(step)}]");
            didLogEdgeRotate = true;
        }
    }


    private float EdgeAttachTargetZForSurface(Surface4 s)
    {
        switch (s)
        {
            case Surface4.Floor: return 0f;
            case Surface4.Ceiling: return 180f;
            case Surface4.RightWall: return 270f;
            case Surface4.LeftWall: return 90f;
            default: return 0f;
        }
    }

    private bool LocalAdhesionProbe(out RaycastHit2D hit)
    {
        var groundT = enemy.GetGroundCheck();
        if (groundT == null) { hit = default; return false; }

        Vector2 origin = (Vector2)groundT.position;

        // IMPORTANT: trust the discrete step surface (not the visual-smoothing normal)
        Vector2 dir = -SurfaceNormal(step.surface);   // cast INTO the current surface defined by step

        // On walls, bias the ray origin slightly INTO the wall so 1-frame gaps don’t read as “edge”
        if (step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall)
            origin += dir * (SKIN_NUDGE + 0.02f);

        float halfExtent = ComputeColliderExtentAlong(dir);
        float dist = Mathf.Max(
            enemy.GetGroundCheckDistance(),
            halfExtent + EDGE_CLEAR_PADDING + 0.1f
        );

        hit = Physics2D.Raycast(origin, dir, dist, enemy.GetWhatIsGround());
        return hit.collider != null;
    }


    private bool LocalAdhesionProbeAgainst(Surface4 s, out RaycastHit2D hit)
    {
        var groundT = enemy.GetGroundCheck();
        if (groundT == null) { hit = default; return false; }

        Vector2 origin = (Vector2)groundT.position;
        Vector2 dir = inAttachPose ? -(Vector2)enemy.transform.up : -SurfaceNormal(s);

        // --- NEW: same wall bias so Attach/Lock doesn’t false-drop on walls ---
        if (!inAttachPose && (s == Surface4.LeftWall || s == Surface4.RightWall))
            origin += dir * (SKIN_NUDGE + 0.02f);

        float halfExtent = ComputeColliderExtentAlong(dir);
        float dist = Mathf.Max(enemy.GetGroundCheckDistance(),
                               halfExtent + EDGE_CLEAR_PADDING + 0.1f);

        hit = Physics2D.Raycast(origin, dir, dist, enemy.GetWhatIsGround());
        return hit.collider != null;
    }

    private bool LocalAdhesionProbeConfirm(out RaycastHit2D hit)
    {
        var groundT = enemy.GetGroundCheck();
        if (groundT == null) { hit = default; return false; }

        Vector2 origin = (Vector2)groundT.position;
        Vector2 dir = -(Vector2)enemy.transform.up; // into the current (new) surface

        // --- NEW: when we’re on a wall, bias into the wall to avoid a one-frame miss after defer ---
        if (step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall)
            origin += dir * (SKIN_NUDGE + 0.02f);

        float halfExtent = ComputeColliderExtentAlong(dir);
        float dist = Mathf.Max(enemy.GetGroundCheckDistance(),
                               halfExtent + EDGE_CLEAR_PADDING + 0.1f);

        hit = Physics2D.Raycast(origin, dir, dist, enemy.GetWhatIsGround());
        return hit.collider != null;
    }

    #endregion

    #region Helper Methods
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
