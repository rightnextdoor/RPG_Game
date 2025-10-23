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

    private enum EdgeWrapPhase { Adhered, EdgeClear, Descend }
    private EdgeWrapPhase wrapPhase = EdgeWrapPhase.Adhered;

    private Vector2 currentNormal = Vector2.up;
    private float savedGravity;
    private float wrapRemaining;

    private const float EDGE_CLEAR_FACTOR = 0.6f;
    private const float SKIN_NUDGE = 0.02f;

    // keep this; we map step→crawlSense so ApplyVelocityCrawler stays unchanged
    private int crawlSense = +1;
    #endregion

    // ---- DEBUG helpers for Surface Crawler ----
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

    private void LogCrawlerState(string prefix)
    {
        Debug.Log($"[Crawler] {enemy.name}: {prefix} | on {SurfaceToString(step.surface)} moving {StepDirToString(step)}");
    }

    private void LogCrawlerTransition(string eventType, CrawlStep from, CrawlStep to)
    {
        Debug.Log(
            $"[Crawler] {enemy.name}: {eventType} -> " +
            $"FROM [{SurfaceToString(from.surface)} {StepDirToString(from)}] " +
            $"TO [{SurfaceToString(to.surface)} {StepDirToString(to)}]"
        );
    }



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

            if (LocalAdhesionProbe(out var hit))
                AlignToSurface(hit);
            else
                AlignToNormal(currentNormal);

            step = new CrawlStep(ClassifySurface(currentNormal), (enemy.facingDir >= 0) ? +1 : -1);
            ApplyStepCrawlSense();

            EnsureFacingMatchesStep();

            LogCrawlerState("Enter");

            wrapPhase = EdgeWrapPhase.Adhered;
            wrapRemaining = 0f;
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

            wrapPhase = EdgeWrapPhase.Adhered;
            wrapRemaining = 0f;
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
        float speed = enemy.moveSpeed * enemy.moveSpeedMultiplier;

        ApplyVelocityCrawler();

        bool adhered = LocalAdhesionProbe(out var hitDown);
        bool wallHit = adhered && LocalWallProbe(out var _);

        if (wallHit)
        {
#if UNITY_EDITOR
            Debug.Log($"[Crawler][Trigger] {enemy.name} | WALL | surf={SurfaceToString(step.surface)} | step.dir={step.dir} | facingDir={enemy.facingDir} | IsFacingRight={enemy.IsFacingRight()}");
#endif
            var next = AdvanceOnWallHit(step);
            LogCrawlerTransition("WALL", step, next);
            SetStepImmediate(next);
            LogCrawlerState("After WALL");
            return;
        }

        if (!adhered)
        {
#if UNITY_EDITOR
            Debug.Log($"[Crawler][Trigger] {enemy.name} | EDGE | surf={SurfaceToString(step.surface)} | step.dir={step.dir} | facingDir={enemy.facingDir} | IsFacingRight={enemy.IsFacingRight()}");
#endif
            var next = AdvanceOnEdge(step);
            LogCrawlerTransition("EDGE", step, next);
            BeginEdgeWrap(next);

            if (TickEdgeWrap(speed))
            {
                return;
            }
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

        Vector2 tangent = new Vector2(currentNormal.y, -currentNormal.x);

        tangent *= crawlSense;

        rb.linearVelocity = tangent.normalized * speed;
    }

    #endregion

    #region Surface Crawler Helpers

    private void BeginEdgeWrap(CrawlStep target)
    {
        pendingSurface = target.surface;
        pendingDir = target.dir;

        wrapRemaining = Mathf.Max(0.01f, enemy.GetWallCheckDistance()) * EDGE_CLEAR_FACTOR;
        wrapPhase = EdgeWrapPhase.EdgeClear;
    }

    private bool TickEdgeWrap(float speed)
    {
        switch (wrapPhase)
        {
            case EdgeWrapPhase.EdgeClear:
                {
                    Vector2 forward = StepForward(step);
                    float stepThis = Mathf.Min(wrapRemaining, speed * Time.deltaTime);

                    rb.linearVelocity = (stepThis > 0f) ? forward * (stepThis / Time.deltaTime) : Vector2.zero;
                    enemy.transform.position += (Vector3)(forward * stepThis);
                    wrapRemaining -= stepThis;

                    if (wrapRemaining > 0f) return true;

                    AlignToNormal(SurfaceNormal(pendingSurface));
                    wrapPhase = EdgeWrapPhase.Descend;
                    return true;
                }

            case EdgeWrapPhase.Descend:
                {
                    Vector2 intoSurface = (-SurfaceNormal(pendingSurface)).normalized;
                    rb.linearVelocity = intoSurface * speed;

                    if (LocalAdhesionProbe(out var hitDown))
                    {
                        AlignToSurface(hitDown);
                        enemy.transform.position += (Vector3)((-hitDown.normal).normalized * SKIN_NUDGE);

                        var from = step;
                        var to = new CrawlStep(pendingSurface, pendingDir);
                        LogCrawlerTransition("EDGE-LOCK", from, to);

                        SetStepImmediate(to);

                        LogCrawlerState("After EDGE-LOCK");

                        wrapPhase = EdgeWrapPhase.Adhered;
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
#if UNITY_EDITOR
            Debug.Log($"[Crawler][Facing] {enemy.name} | surf=Floor | step.dir={step.dir} | facingDir={enemy.facingDir} | IsFacingRight={enemy.IsFacingRight()}");
#endif
            return;
        }

        if (step.surface == Surface4.Ceiling)
        {
            bool wantRightOnCeiling = (step.dir < 0);
            if (enemy.IsFacingRight() != wantRightOnCeiling) enemy.Flip();
#if UNITY_EDITOR
            Debug.Log($"[Crawler][Facing] {enemy.name} | surf=Ceiling | step.dir={step.dir} | facingDir={enemy.facingDir} | IsFacingRight={enemy.IsFacingRight()}");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[Crawler][Facing] {enemy.name} | surf={SurfaceToString(step.surface)} | step.dir={step.dir} | facingDir={enemy.facingDir} | IsFacingRight={enemy.IsFacingRight()}");
#endif
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
            // Floor edge → descend the side wall
            case Surface4.Floor:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, -1)   // Floor Right → RightWall Down
                                   : new CrawlStep(Surface4.LeftWall, -1);  // Floor Left  → LeftWall  Down

            // Ceiling edge → ascend the side wall
            case Surface4.Ceiling:
                return (s.dir > 0) ? new CrawlStep(Surface4.RightWall, +1)   // Ceiling Right → RightWall Up
                                   : new CrawlStep(Surface4.LeftWall, +1);  // Ceiling Left  → LeftWall  Up

            // Wall edge → cap to plane by vertical direction
            case Surface4.RightWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Ceiling, -1)     // RightWall Up   → Ceiling Left
                                   : new CrawlStep(Surface4.Floor, -1);     // RightWall Down → Floor   Left

            case Surface4.LeftWall:
                return (s.dir > 0) ? new CrawlStep(Surface4.Ceiling, +1)     // LeftWall Up    → Ceiling Right
                                   : new CrawlStep(Surface4.Floor, +1);     // LeftWall Down  → Floor   Right
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

#if UNITY_EDITOR
        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            string hitName = (hit.collider != null) ? hit.collider.name : "none";
            Debug.Log($"[Crawler][WallProbe] {enemy.name} | surf={SurfaceToString(step.surface)} dir={StepDirToString(step)} | rayDir={dir} dist={dist} | hit={hitName}");
        }
#endif

        return hit.collider != null;
    }

    private void AlignToSurface(RaycastHit2D hit)
    {
        AlignToNormal((hit.normal.sqrMagnitude > 0.0001f) ? hit.normal.normalized : Vector2.up);
    }

    private void AlignToNormal(Vector2 normal)
    {
        currentNormal = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector2.up;

        // Base Z from the surface normal (feet toward +normal)
        float angleZ = Vector2.SignedAngle(Vector2.up, currentNormal);

        // 🔧 Only tweak Z when crawling on a WALL and the enemy is moving LEFT
        // (i.e., not facing right). Floor/Ceiling remain driven by Flip().
        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            bool onWall = step.surface == Surface4.LeftWall || step.surface == Surface4.RightWall;
            if (onWall && !enemy.IsFacingRight())
            {
                angleZ += 180f; // flip feet side for the “move-left” wall case
            }
        }

        var e = enemy.transform.eulerAngles;
        e.z = Normalize360(angleZ);
        enemy.transform.eulerAngles = e;
    }

    private static float Normalize360(float a)
    {
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    private bool LocalAdhesionProbe(out RaycastHit2D hit)
    {
        var groundT = enemy.GetGroundCheck();
        if (groundT == null) { hit = default; return false; }

        Vector2 origin = (Vector2)groundT.position;

        // KEY CHANGE: trust the step, not transform.up
        Vector2 dir = -SurfaceNormal(step.surface);   // into the surface we believe we’re on

        float dist = Mathf.Max(0.01f, enemy.GetGroundCheckDistance());
        hit = Physics2D.Raycast(origin, dir, dist, enemy.GetWhatIsGround());

#if UNITY_EDITOR
        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            string hitName = (hit.collider != null) ? hit.collider.name : "none";
            Vector2 liveDown = -(Vector2)enemy.transform.up;
            Debug.Log($"[Crawler][Adhesion] {enemy.name} | surf={SurfaceToString(step.surface)} dir={StepDirToString(step)} | expectedRay={dir} liveRay={liveDown} dist={dist} | hit={hitName}");
        }
#endif

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
