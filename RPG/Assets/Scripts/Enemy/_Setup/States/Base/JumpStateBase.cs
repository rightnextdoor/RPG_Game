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

            var next = nextProvider != null ? nextProvider() : null;
            if (next != null)
                stateMachine.ChangeState(next);
        }
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

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var xf = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(xf);
    }
}