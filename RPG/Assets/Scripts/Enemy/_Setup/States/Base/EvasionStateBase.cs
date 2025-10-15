using System;
using System.Collections.Generic;
using UnityEngine;

public class EvasionStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    public enum Intent { MoveAndAttack, Flee }

    private readonly Func<EnemyState> defaultNextState;
    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private Func<EnemyState> nextState;
    private Func<EnemyState> attackState;
    private float? rangeMin, rangeMax;
    private Intent intent;
    private bool configured;

    private struct Attempt { public int kind; public int dir; public float dist; }
    private readonly List<Attempt> attempts = new List<Attempt>(4);
    private int attemptIndex;
    private float travelRemaining;
    private int moveDir;
    private bool bounceMode;
    private int primaryKind;

    private static readonly List<StateSound> _empty = new List<StateSound>();

    public EvasionStateBase(
        TEnemy enemy,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> battleState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(enemy, stateMachine, animBoolName)
    {
        defaultNextState = battleState;
        this.enterSounds = enterSounds ?? _empty;
        this.exitSounds = exitSounds ?? _empty;
    }

    public void ConfigureMoveAndAttack(Func<EnemyState> next, Func<EnemyState> attack, float? min, float? max)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));
        if (attack == null) throw new ArgumentNullException(nameof(attack));

        intent = Intent.MoveAndAttack;
        nextState = next;
        attackState = attack;
        rangeMin = min;
        rangeMax = max;
        configured = true;


    }

    public void ConfigureFlee(Func<EnemyState> next)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));

        intent = Intent.Flee;
        nextState = next;
        attackState = null;
        rangeMin = rangeMax = null;
        configured = true;

    }

    public override void Enter()
    {
        base.Enter();

        if (!configured)
        {
            stateMachine.ChangeState(defaultNextState());
            return;
        }

        stateTimer = enemy.evasionDuration;
        bounceMode = false;
        attempts.Clear();

        primaryKind = UnityEngine.Random.Range(0, 2);

        BuildAttempts();
        attemptIndex = 0;

        if (!StartAttempt(attemptIndex))
        {
            bounceMode = true;
            moveDir = (primaryKind == 0 ? -TowardPlayerDir() : TowardPlayerDir());
            EnsureFacingForMovement(moveDir, 1);
        }

        PlayStateSounds(enterSounds);
    }

    public override void Update()
    {
        base.Update();

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(nextState());
            return;
        }

        if (bounceMode) TickBounce();
        else TickEscape();
    }

    public override void Exit()
    {
        base.Exit();
        enemy.SetZeroVelocity();
        PlayStateSounds(exitSounds);
        configured = false;
    }

    private void TickEscape()
    {
        float speed = enemy.moveSpeed * enemy.evasionSpeedMultiplier * moveDir;
        enemy.SetVelocity(speed, rb.linearVelocity.y);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            attemptIndex++;
            if (!StartAttempt(attemptIndex))
            {
                bounceMode = true;
                return;
            }
            return;
        }

        travelRemaining -= Mathf.Abs(speed) * Time.deltaTime;
        if (travelRemaining > 0f) return;

        if (intent == Intent.MoveAndAttack)
        {
            FacePlayerNow();

            stateMachine.ChangeState(attackState());
        }
        else
        {
            stateMachine.ChangeState(nextState());
        }
    }

    private void TickBounce()
    {
        float speed = enemy.moveSpeed * enemy.evasionSpeedMultiplier * moveDir;
        enemy.SetVelocity(speed, rb.linearVelocity.y);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            moveDir = -moveDir;
            EnsureFacingForMovement(moveDir, 1);
        }
    }

    private void BuildAttempts()
    {
        var p = PlayerUtils.GetPlayerSafe();
        float dx = 0f;
        if (p != null) dx = Mathf.Abs(p.transform.position.x - enemy.transform.position.x);

        float primaryFull, alternateFull;
        ComputeFullDistances(dx, out primaryFull, out alternateFull);

        int toward = TowardPlayerDir();
        int away = -toward;
        int alt = 1 - primaryKind;

        // Primary full
        attempts.Add(new Attempt
        {
            kind = primaryKind,
            dir = (primaryKind == 0 ? away : toward),
            dist = Mathf.Max(0.05f, primaryFull)
        });

        // Alternate full
        attempts.Add(new Attempt
        {
            kind = alt,
            dir = (alt == 0 ? away : toward),
            dist = Mathf.Max(0.05f, alternateFull)
        });

        // Primary reduced
        attempts.Add(new Attempt
        {
            kind = primaryKind,
            dir = (primaryKind == 0 ? away : toward),
            dist = Mathf.Max(0.05f, primaryFull * Mathf.Clamp01(enemy.evadeReducedFactor))
        });

        // Alternate reduced
        attempts.Add(new Attempt
        {
            kind = alt,
            dir = (alt == 0 ? away : toward),
            dist = Mathf.Max(0.05f, alternateFull * Mathf.Clamp01(enemy.evadeReducedFactor))
        });
    }

    private void ComputeFullDistances(float dx, out float primaryFull, out float alternateFull)
    {
        // Defaults (no band or Flee)
        float backFull = UnityEngine.Random.Range(
            Mathf.Min(enemy.evadeBackAwayMin, enemy.evadeBackAwayMax),
            Mathf.Max(enemy.evadeBackAwayMin, enemy.evadeBackAwayMax)
        );
        float passFull = dx + UnityEngine.Random.Range(
            Mathf.Min(enemy.evadePassPastMin, enemy.evadePassPastMax),
            Mathf.Max(enemy.evadePassPastMin, enemy.evadePassPastMax)
        );

        if (intent == Intent.MoveAndAttack && rangeMin.HasValue && rangeMax.HasValue)
        {
            float min = rangeMin.Value;
            float max = rangeMax.Value;

            float backDist = (dx < min) ? (min - dx) : Mathf.Max(0.05f, enemy.evadeTinyRetreat);

            float mid = (min + max) * 0.5f;
            float passDist = dx + Mathf.Max(0.05f, mid);

            if (primaryKind == 0) { primaryFull = backDist; alternateFull = passDist; }
            else { primaryFull = passDist; alternateFull = backDist; }
        }
        else
        {
            if (primaryKind == 0) { primaryFull = backFull; alternateFull = passFull; }
            else { primaryFull = passFull; alternateFull = backFull; }
        }
    }

    private bool StartAttempt(int idx)
    {
        if (idx < 0 || idx >= attempts.Count) return false;

        var a = attempts[idx];
        moveDir = a.dir;
        travelRemaining = a.dist;

        EnsureFacingForMovement(moveDir, a.kind);
        return true;
    }

    private int TowardPlayerDir()
    {
        var p = PlayerUtils.GetPlayerSafe();
        if (p == null) return enemy.facingDir;
        float dx = p.transform.position.x - enemy.transform.position.x;
        if (Mathf.Abs(dx) < 0.001f) return enemy.facingDir;
        return dx > 0f ? 1 : -1;
    }

    private void EnsureFacingForMovement(int dir, int kind)
    {
        if (kind == 1)
        {
            if (enemy.facingDir != dir) enemy.Flip();
        }
        else
        {
            if (intent == Intent.MoveAndAttack)
            {
                int toward = TowardPlayerDir();
                if (enemy.facingDir != toward) enemy.Flip();
            }
            else
            {
                if (enemy.facingDir != dir) enemy.Flip();
            }
        }
    }

    private void FacePlayerNow()
    {
        int toward = TowardPlayerDir();
        if (enemy.facingDir != toward) enemy.Flip();
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var src = enemy.transform;
        for (int i = 0; i < sounds.Count; i++) sounds[i].Play(src);
    }
}
