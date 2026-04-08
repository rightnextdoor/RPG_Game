using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunIntoPlayerMode
{
    Hit
}

public class RunIntoPlayerStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private static readonly List<StateSound> Empty = new();

    private Func<EnemyState> nextState;
    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private RunIntoPlayerMode mode;

    public RunIntoPlayerStateBase(
        TEnemy enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> nextState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName)
    {
        this.nextState = nextState;
        this.enterSounds = enterSounds ?? Empty;
        this.exitSounds = exitSounds ?? Empty;
    }

    #region Configure
    public virtual void Configure(RunIntoPlayerMode mode, Func<EnemyState> next)
    {
        this.mode = mode;
        nextState = next;
    }

    public virtual void SetNextState(Func<EnemyState> next)
    {
        nextState = next;
    }
    #endregion

    #region State
    public override void Enter()
    {
        base.Enter();
        PlayStateSounds(enterSounds);
        SetupMode();
    }

    public override void Update()
    {
        base.Update();
        UpdateMode();
    }

    public override void Exit()
    {
        base.Exit();
        PlayStateSounds(exitSounds);
    }
    #endregion

    #region Setup
    private void SetupMode()
    {
        switch (mode)
        {
            case RunIntoPlayerMode.Hit:
                SetupHitMode();
                break;
        }
    }

    protected virtual void SetupHitMode()
    {
        stateTimer = 0.05f;
    }
    #endregion

    #region Update logic
    private void UpdateMode()
    {
        switch (mode)
        {
            case RunIntoPlayerMode.Hit:
                UpdateHitMode();
                break;
        }
    }

    protected virtual void UpdateHitMode()
    {
        UpdateVelocity();

        if (TryGetPlayerHit(out PlayerStats target))
        {
            DoDamage(target);
            ChangeToNextState();
            return;
        }

        if (stateTimer <= 0f)
            ChangeToNextState();
    }
    #endregion

    #region Helpers
    protected virtual void UpdateVelocity()
    {
        float xVelocity = enemy.moveSpeed * enemy.battleSpeedMultiplier * enemy.facingDir;
        enemy.SetVelocity(xVelocity, rb.linearVelocity.y);
    }

    protected virtual bool TryGetPlayerHit(out PlayerStats target)
    {
        target = null;

        var player = PlayerUtils.GetPlayerSafe();
        if (player == null)
            return false;

        CapsuleCollider2D capsule = enemy.GetComponent<CapsuleCollider2D>();
        if (capsule == null)
            return false;

        int playerLayer = player.gameObject.layer;

        Vector2 center = capsule.bounds.center;
        Vector2 size = capsule.bounds.size;
        float angle = enemy.transform.eulerAngles.z;

        float moveStep = enemy.moveSpeed * enemy.battleSpeedMultiplier * Time.deltaTime;
        float hitPadding = 0.15f;
        float castDistance = moveStep + hitPadding;

        Vector2 castDir = new Vector2(enemy.facingDir, 0f);

        Vector2 sweepCenter = center + (castDir * (castDistance * 0.5f));
        Vector2 sweepSize = size;
        sweepSize.x += castDistance;

        Collider2D[] overlaps = Physics2D.OverlapCapsuleAll(
            sweepCenter,
            sweepSize,
            capsule.direction,
            angle
        );

        if (overlaps == null || overlaps.Length == 0)
            return false;

        foreach (Collider2D hit in overlaps)
        {
            if (hit == null || hit.transform == enemy.transform)
                continue;

            if (hit.gameObject.layer != playerLayer)
                continue;

            target = hit.GetComponent<PlayerStats>();
            if (target != null)
                return true;
        }

        return false;
    }

    protected virtual void DoDamage(PlayerStats target)
    {
        if (target == null)
            return;

        enemy.stats.DoDamage(target);
    }

    protected virtual void ChangeToNextState()
    {
        var next = nextState != null ? nextState() : null;
        if (next != null)
            stateMachine.ChangeState(next);
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0)
            return;

        var xf = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(xf);
    }
    #endregion
}