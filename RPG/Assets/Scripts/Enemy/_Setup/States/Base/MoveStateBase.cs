using UnityEngine;
using static Enemy_Regular;

public class MoveStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private readonly System.Func<EnemyState> idleState;
    private readonly System.Func<EnemyState> battleState;

    private float savedGravity;
    private Vector2 currentNormal = Vector2.up;
    private bool hasSurface;
    private float traverseStopTimer;
    private bool needsPatrolReturn;

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

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            savedGravity = rb.gravityScale;
            rb.gravityScale = 0f;

            hasSurface = TryAcquireSurface(out RaycastHit2D hit);
            if (hasSurface)
                AlignToSurface(hit);
        }

        ApplyVelocity();
    }

    public override void Update()
    {
        base.Update();

        if (needsPatrolReturn)
        {
            needsPatrolReturn = ReturnToPatrolCenterIfOutside();
            return;
        }

        if (battleState != null && PlayerDetectedValid())
        {
            stateMachine.ChangeState(battleState());
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

            case RegularMoveMode.SurfaceCrawler:
                SurfaceCrawler();
                break;
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            rb.gravityScale = savedGravity;
            hasSurface = false;
            currentNormal = Vector2.up;
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
        if (!hasSurface)
        {
            hasSurface = TryAcquireSurface(out RaycastHit2D reacquire);
            if (hasSurface) AlignToSurface(reacquire);
        }

        ApplyVelocityCrawler();

        Vector2 origin = enemy.transform.position;
        Vector2 tangent = (Vector2)enemy.transform.right * Mathf.Sign(enemy.facingDir);

        var forwardHit = Physics2D.Raycast(origin, tangent, Mathf.Max(0.05f, enemy.surfaceProbeAhead), enemy.surfaceCrawlMask);
        DebugDraw(origin, tangent * Mathf.Max(0.05f, enemy.surfaceProbeAhead), Color.cyan);

        if (!forwardHit)
        {
            Vector2 wrapStart = origin + tangent * Mathf.Max(0.05f, enemy.surfaceProbeAhead);
            var wrapHit = Physics2D.Raycast(wrapStart, -currentNormal, Mathf.Max(0.05f, enemy.surfaceProbeDown), enemy.surfaceCrawlMask);
            DebugDraw(wrapStart, -currentNormal * Mathf.Max(0.05f, enemy.surfaceProbeDown), Color.yellow);

            if (wrapHit) AlignToSurface(wrapHit);
            else hasSurface = false;
        }
        else
        {
            Vector2 downStart = origin + tangent * (Mathf.Max(0.05f, enemy.surfaceProbeAhead) * 0.5f);
            var downHit = Physics2D.Raycast(downStart, -currentNormal, Mathf.Max(0.05f, enemy.surfaceProbeDown), enemy.surfaceCrawlMask);
            DebugDraw(downStart, -currentNormal * Mathf.Max(0.05f, enemy.surfaceProbeDown), Color.green);
            if (downHit) AlignToSurface(downHit);
        }

        if (IsOutsidePatrolX(enemy.transform.position.x))
        {
            enemy.Flip();
            ApplyVelocityCrawler();
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
        float s = enemy.moveSpeed * enemy.moveSpeedMultiplier;
        Vector2 tangential = (Vector2)enemy.transform.right * Mathf.Sign(enemy.facingDir) * s;
        rb.linearVelocity = tangential;
    }

    #endregion

    #region Surface Crawler Helpers

    private bool TryAcquireSurface(out RaycastHit2D hit)
    {
        Vector2 origin = enemy.transform.position;
        int mask = enemy.surfaceCrawlMask;

        Vector2[] dirs =
        {
            Vector2.down, Vector2.up, Vector2.left, Vector2.right,
            (Vector2.down + Vector2.left).normalized,
            (Vector2.down + Vector2.right).normalized,
            (Vector2.up + Vector2.left).normalized,
            (Vector2.up + Vector2.right).normalized
        };

        float probe = Mathf.Max(Mathf.Max(0.05f, enemy.surfaceProbeAhead), Mathf.Max(0.05f, enemy.surfaceProbeDown));
        float best = float.MaxValue;
        RaycastHit2D bestHit = default;

        foreach (var d in dirs)
        {
            var h = Physics2D.Raycast(origin, d, probe, mask);
            DebugDraw(origin, d * probe, new Color(1f, 0.5f, 0f, 0.5f));
            if (h && h.distance < best)
            {
                best = h.distance;
                bestHit = h;
            }
        }

        if (!bestHit)
        {
            var c = Physics2D.CircleCast(origin, 0.2f, Vector2.zero, 0f, mask);
            if (c) bestHit = c;
        }

        hit = bestHit;
        return bestHit;
    }

    private void AlignToSurface(RaycastHit2D hit)
    {
        currentNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector2.up;

        // rotate transform so its up aligns to the surface normal; preserve Y flip only
        Quaternion toSurface = Quaternion.FromToRotation(enemy.transform.up, currentNormal);
        enemy.transform.rotation = toSurface * enemy.transform.rotation;

        var eul = enemy.transform.eulerAngles;
        enemy.transform.rotation = Quaternion.Euler(0f, eul.y, 0f);

        // ensure facing matches tangential direction
        Vector2 tangent = enemy.transform.right;
        float dot = Vector2.Dot(tangent, Vector2.right * enemy.facingDir);
        if (dot < 0f) enemy.Flip();

        hasSurface = true;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DebugDraw(Vector2 start, Vector2 vec, Color c)
    {
#if UNITY_EDITOR
        Debug.DrawLine(start, start + vec, c, 0f);
#endif
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
