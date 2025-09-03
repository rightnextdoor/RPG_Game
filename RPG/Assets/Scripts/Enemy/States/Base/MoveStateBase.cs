using UnityEngine;
using static Enemy_Regular;

public class MoveStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private readonly System.Func<EnemyState> idleState;
    private readonly System.Func<EnemyState> battleState;

    private float nextStopTimer;
    private float stopTimer;
    private bool isStopping;

    private float leftX, rightX;

    private float savedGravity;
    private Vector2 currentNormal = Vector2.up;
    private bool hasSurface;

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

        if (enemy.randomizeFacingOnMoveEnter && Random.value < 0.5f)
            enemy.Flip();

        ComputePatrolRect();

        ScheduleNextStop();

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

        if (enemy.allowBattleInterruptFromMove && battleState != null && PlayerDetectedValid())
        {
            stateMachine.ChangeState(battleState());
            return;
        }

        switch (enemy.moveMode)
        {
            case RegularMoveMode.PatrolArea:
                TickPatrolArea();
                break;

            case RegularMoveMode.TraverseNoStops:
                TickTraverse();
                break;

            case RegularMoveMode.SurfaceCrawler:
                TickSurfaceCrawler();
                break;
        }
    }

    public override void Exit()
    {
        base.Exit();

        isStopping = false;
        stopTimer = 0f;

        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            rb.gravityScale = savedGravity;
            hasSurface = false;
            currentNormal = Vector2.up;
        }
    }


    private void TickPatrolArea()
    {
        if (!CanMoveNow())
        {
            enemy.SetZeroVelocity();
            return;
        }

        if (enemy.moveStopsEnabled)
        {
            if (!isStopping)
            {
                nextStopTimer -= Time.deltaTime;
                if (nextStopTimer <= 0f)
                {
                    isStopping = true;
                    stopTimer = Random.Range(enemy.moveStopDurationRange.x, enemy.moveStopDurationRange.y);
                    enemy.SetZeroVelocity();
                }
            }
            else
            {
                stopTimer -= Time.deltaTime;
                enemy.SetZeroVelocity();

                if (stopTimer <= 0f)
                {
                    isStopping = false;
                    if (Random.value < enemy.flipOnStopChance) enemy.Flip();
                    ScheduleNextStop();
                }
                return;
            }
        }

        ApplyVelocity();

        if (IsOutsidePatrolX(enemy.transform.position.x) || enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            OnBoundary();
            return;
        }
    }

    private void TickTraverse()
    {
        if (!CanMoveNow())
        {
            enemy.SetZeroVelocity();
            return;
        }

        ApplyVelocity();

        if (IsOutsidePatrolX(enemy.transform.position.x) || enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            OnBoundary();
            return;
        }
    }

    private void TickSurfaceCrawler()
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

    private void ApplyVelocity()
    {
        if (enemy.moveMode == RegularMoveMode.SurfaceCrawler)
        {
            ApplyVelocityCrawler();
            return;
        }

        float vx = enemy.moveSpeed * enemy.moveSpeedMultiplier * enemy.facingDir;
        enemy.SetVelocity(vx, rb.velocity.y);
    }

    private void ApplyVelocityCrawler()
    {
        float s = enemy.moveSpeed * enemy.moveSpeedMultiplier;
        Vector2 tangential = (Vector2)enemy.transform.right * Mathf.Sign(enemy.facingDir) * s;
        rb.velocity = tangential;
    }

    private void OnBoundary()
    {
        if (enemy.enterIdleOnPatrolBoundary && idleState != null)
        {
            enemy.Flip();
            stateMachine.ChangeState(idleState());
        }
        else
        {
            enemy.Flip();
            ApplyVelocity();
        }
    }

    private bool PlayerDetectedValid()
    {
        if (enemy.IsPlayerDetected()) return true;

        var player = PlayerUtils.GetPlayerSafe();
        if (player == null) return false;

        return Vector2.Distance(enemy.transform.position, player.transform.position) < enemy.agroDistance;
    }

    private bool CanMoveNow()
    {
        return enemy.canPatrol;
    }

    private void ComputePatrolRect()
    {
        Vector3 center = enemy.patrolAreaAnchor ? enemy.patrolAreaAnchor.position : enemy.transform.position;
        Vector2 half = enemy.patrolAreaSize * 0.5f;
        leftX = center.x - half.x;
        rightX = center.x + half.x;
    }

    private bool IsOutsidePatrolX(float x) => x < leftX || x > rightX;

    private void ScheduleNextStop()
    {
        nextStopTimer = Random.Range(enemy.moveStopIntervalRange.x, enemy.moveStopIntervalRange.y);
    }

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

#if UNITY_EDITOR
    // draws patrol area without adding inspector toggles
    protected virtual void OnDrawGizmosSelected()
    {
        if (enemy == null) return;

        if (enemy.moveMode == RegularMoveMode.PatrolArea ||
            enemy.moveMode == RegularMoveMode.TraverseNoStops)
        {
            Vector3 center = enemy.patrolAreaAnchor ? enemy.patrolAreaAnchor.position : enemy.transform.position;
            Vector3 size = enemy.patrolAreaSize;

            Gizmos.color = new Color(0f, 1f, 0f, 0.12f);
            Gizmos.DrawCube(center, size);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}
