using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private static readonly List<StateSound> Empty = new();

    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private Func<EnemyState> nextProvider;
    private Vector2 configuredJumpVelocity;
    private bool configuredIsBack;

    private bool hasLeftGround;

    public JumpStateBase(
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

    public virtual void Configure(
        Func<EnemyState> next,
        Vector2 jumpVelocity,
        bool isJumpBack
    )
    {
        nextProvider = next;
        configuredJumpVelocity = jumpVelocity;
        configuredIsBack = isJumpBack;
    }

    public override void Enter()
    {
        base.Enter();

        PlayStateSounds(enterSounds);

        hasLeftGround = false;

        if (!CanJumpSafely())
        {
            enemy.SetZeroVelocity();
            ChangeToNextState();
            return;
        }

        if (configuredIsBack)
            JumpBack();
        else
            JumpForward();
    }

    public override void Update()
    {
        base.Update();

        if (enemy.anim != null)
            enemy.anim.SetFloat("yVelocity", rb.linearVelocity.y);

        UpdateJumpLanding();
    }

    public override void Exit()
    {
        base.Exit();
        PlayStateSounds(exitSounds);
    }

    protected virtual void JumpBack()
    {
        float dir = -enemy.facingDir;
        rb.linearVelocity = new Vector2(configuredJumpVelocity.x * dir, configuredJumpVelocity.y);
    }

    protected virtual void JumpForward()
    {
        float dir = enemy.facingDir;
        rb.linearVelocity = new Vector2(configuredJumpVelocity.x * dir, configuredJumpVelocity.y);
    }

    protected virtual void UpdateJumpLanding()
    {
        bool hasFloorContact = HasFloorContact();

        if (!hasLeftGround)
        {
            if (!hasFloorContact)
                hasLeftGround = true;

            return;
        }

        if (hasFloorContact)
        {
            enemy.SetZeroVelocity();
            ChangeToNextState();
        }
    }

    protected virtual bool CanJumpSafely()
    {
        if (enemy.cd == null)
            return true;

        Bounds bounds = enemy.cd.bounds;
        LayerMask groundMask = enemy.GetWhatIsGround();

        float jumpDirection = configuredIsBack ? -enemy.facingDir : enemy.facingDir;
        float estimatedDistance = EstimateJumpDistance();
        float landingPadding = Mathf.Max(0.15f, bounds.extents.x * 0.35f);

        float targetX = bounds.center.x + jumpDirection * estimatedDistance;

        float searchHeight = Mathf.Max(1f, bounds.size.y + Mathf.Abs(configuredJumpVelocity.y) * 0.15f + 0.5f);
        Vector2 groundSearchStart = new Vector2(targetX, bounds.max.y + searchHeight);

        if (!TryFindGroundBelow(groundSearchStart, bounds.size.y + searchHeight * 2f, groundMask, out RaycastHit2D groundHit))
            return false;

        float bodyHeightOffset = bounds.extents.y + 0.05f;
        Vector2 landingCenter = new Vector2(targetX, groundHit.point.y + bodyHeightOffset);

        if (SomethingIsAround(landingCenter, bounds, landingPadding, groundMask))
            return false;

        if (!HasFullGroundSupport(landingCenter, bounds, landingPadding, groundMask))
            return false;

        if (HitsWallOnPath(bounds, landingCenter, groundMask))
            return false;

        return true;
    }

    protected virtual float EstimateJumpDistance()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);

        if (gravity <= 0.0001f)
            return Mathf.Abs(configuredJumpVelocity.x);

        float airTime = (2f * Mathf.Max(0f, configuredJumpVelocity.y)) / gravity;
        float distance = Mathf.Abs(configuredJumpVelocity.x) * airTime;

        return Mathf.Max(boundsWidthFallback(), distance);
    }

    private float boundsWidthFallback()
    {
        if (enemy.cd == null)
            return 0.5f;

        return enemy.cd.bounds.size.x;
    }

    protected virtual bool TryFindGroundBelow(Vector2 start, float distance, LayerMask groundMask, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(start, Vector2.down, distance, groundMask);
        return hit.collider != null;
    }

    protected virtual bool SomethingIsAround(Vector2 landingCenter, Bounds bounds, float padding, LayerMask groundMask)
    {
        Vector2 areaSize = new Vector2(bounds.size.x + padding * 2f, bounds.size.y * 0.9f);
        Collider2D blocker = Physics2D.OverlapBox(landingCenter, areaSize, 0f, groundMask);

        if (blocker == null)
            return false;

        return blocker != enemy.cd;
    }

    protected virtual bool HasFullGroundSupport(Vector2 landingCenter, Bounds bounds, float padding, LayerMask groundMask)
    {
        float supportWidth = bounds.size.x + padding * 2f;
        float left = landingCenter.x - supportWidth * 0.5f;
        float right = landingCenter.x + supportWidth * 0.5f;

        float castStartY = landingCenter.y - bounds.extents.y + 0.05f;
        float castDistance = 0.35f;

        int samples = 5;
        for (int i = 0; i < samples; i++)
        {
            float t = samples == 1 ? 0.5f : i / (float)(samples - 1);
            float sampleX = Mathf.Lerp(left, right, t);
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(sampleX, castStartY), Vector2.down, castDistance, groundMask);

            if (hit.collider == null)
                return false;
        }

        return true;
    }

    protected virtual bool HitsWallOnPath(Bounds bounds, Vector2 landingCenter, LayerMask groundMask)
    {
        Vector2 start = bounds.center;
        Vector2 end = new Vector2(landingCenter.x, start.y);

        Vector2 direction = end - start;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return false;

        Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.8f);
        RaycastHit2D hit = Physics2D.BoxCast(start, boxSize, 0f, direction.normalized, distance, groundMask);

        if (hit.collider == null)
            return false;

        return hit.collider != enemy.cd;
    }

    protected virtual bool HasFloorContact()
    {
        if (enemy.cd == null)
            return enemy.IsGroundDetected();

        Bounds bounds = enemy.cd.bounds;

        float insetX = Mathf.Min(0.05f, bounds.extents.x * 0.2f);
        float probeWidth = Mathf.Max(0.02f, bounds.size.x - insetX * 2f);
        float probeHeight = 0.06f;

        Vector2 probeSize = new Vector2(probeWidth, probeHeight);
        Vector2 probeCenter = new Vector2(bounds.center.x, bounds.min.y - probeHeight * 0.5f - 0.02f);

        return Physics2D.OverlapBox(probeCenter, probeSize, 0f, enemy.GetWhatIsGround()) != null;
    }

    protected virtual void ChangeToNextState()
    {
        var next = nextProvider != null ? nextProvider() : null;
        if (next != null)
            stateMachine.ChangeState(next);
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var xf = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(xf);
    }
}