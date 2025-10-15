using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private static readonly List<StateSound> Empty = new();

    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private Func<EnemyState> nextProvider;
    private float configuredFallTime;
    private Vector2 configuredJumpVelocity;
    private bool configuredIsBack;

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
        float fallTime,
        Vector2 jumpVelocity,
        bool isJumpBack
    )
    {
        nextProvider = next;
        configuredFallTime = fallTime;
        configuredJumpVelocity = jumpVelocity;
        configuredIsBack = isJumpBack;
    }

    public override void Enter()
    {
        base.Enter();

        PlayStateSounds(enterSounds);

        stateTimer = configuredFallTime;

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

        if (rb.linearVelocity.y < 0f && enemy.IsGroundDetected())
        {
            var next = nextProvider != null ? nextProvider() : null;
            if (next != null)
                stateMachine.ChangeState(next);
            return;
        }

        if (stateTimer < 0f)
        {
            var next = nextProvider != null ? nextProvider() : null;
            if (next != null)
                stateMachine.ChangeState(next);
        }
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

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var xf = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(xf);
    }
}
