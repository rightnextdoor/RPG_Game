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
    private Intent intent;
    private bool configured;

    private struct Attempt { public int kind; public int dir; public float dist; }
    private readonly List<Attempt> attempts = new List<Attempt>(4);
    private int attemptIndex;
    private float travelRemaining;
    private int moveDir;
    private bool bounceMode;
    private int primaryKind;

    private float evasionDuration;
    private float evasionSpeedMultiplier;
    private float evadeBackAwayMin;
    private float evadeBackAwayMax;
    private float evadePassPastMin;
    private float evadePassPastMax;
    private float evadeReducedFactor;

    private float attackMaxRange;
    private float attackStopPercent = 0.8f;
    private float attackStopRange;

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

    public void ConfigureMoveAndAttack(
    Func<EnemyState> next,
    Func<EnemyState> attack,
    AbilityEntry entry,
    AbilityEntry attackEntry)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));
        if (attack == null) throw new ArgumentNullException(nameof(attack));
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (attackEntry == null) throw new ArgumentNullException(nameof(attackEntry));

        intent = Intent.MoveAndAttack;
        nextState = next;
        attackState = attack;

        evasionDuration = entry.evasionDuration;
        evasionSpeedMultiplier = entry.evasionSpeedMultiplier;
        evadeBackAwayMin = entry.evadeBackAwayMin;
        evadeBackAwayMax = entry.evadeBackAwayMax;
        evadePassPastMin = entry.evadePassPastMin;
        evadePassPastMax = entry.evadePassPastMax;
        evadeReducedFactor = entry.evadeReducedFactor;

        attackMaxRange = Mathf.Max(0f, attackEntry.rangeMax ?? 0f);
        attackStopRange = attackMaxRange > 0f
            ? Mathf.Max(0f, attackMaxRange * attackStopPercent)
            : 0f;

        configured = true;
    }

    public void ConfigureFlee(
    Func<EnemyState> next,
    AbilityEntry entry)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        intent = Intent.Flee;
        nextState = next;
        attackState = null;

        evasionDuration = entry.evasionDuration;
        evasionSpeedMultiplier = entry.evasionSpeedMultiplier;
        evadeBackAwayMin = entry.evadeBackAwayMin;
        evadeBackAwayMax = entry.evadeBackAwayMax;
        evadePassPastMin = entry.evadePassPastMin;
        evadePassPastMax = entry.evadePassPastMax;
        evadeReducedFactor = entry.evadeReducedFactor;

        attackMaxRange = 0f;
        attackStopRange = 0f;

        configured = true;
    }

    public virtual void SetNextState(Func<EnemyState> next)
    {
        nextState = next;
    }

    public virtual void SetAttackState(Func<EnemyState> attack)
    {
        attackState = attack;
    }

    public override void Enter()
    {
        base.Enter();

        if (!configured)
        {
            stateMachine.ChangeState(defaultNextState());
            return;
        }

        stateTimer = evasionDuration;
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
        float speed = enemy.moveSpeed * evasionSpeedMultiplier * moveDir;
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

        if (intent == Intent.MoveAndAttack && IsInAttackStopRange())
        {
            enemy.SetZeroVelocity();
            FacePlayerNow();
            stateMachine.ChangeState(attackState());
            return;
        }

        travelRemaining -= Mathf.Abs(speed) * Time.deltaTime;
        if (travelRemaining > 0f)
            return;

        enemy.SetZeroVelocity();

        if (intent == Intent.MoveAndAttack && attackMaxRange <= 0f)
        {
            FacePlayerNow();
            stateMachine.ChangeState(attackState());
            return;
        }

        stateMachine.ChangeState(nextState());
    }

    private void TickBounce()
    {
        float speed = enemy.moveSpeed * evasionSpeedMultiplier * moveDir;
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

        attempts.Add(new Attempt
        {
            kind = primaryKind,
            dir = (primaryKind == 0 ? away : toward),
            dist = Mathf.Max(0.05f, primaryFull)
        });

        attempts.Add(new Attempt
        {
            kind = alt,
            dir = (alt == 0 ? away : toward),
            dist = Mathf.Max(0.05f, alternateFull)
        });

        attempts.Add(new Attempt
        {
            kind = primaryKind,
            dir = (primaryKind == 0 ? away : toward),
            dist = Mathf.Max(0.05f, primaryFull * Mathf.Clamp01(evadeReducedFactor))
        });

        attempts.Add(new Attempt
        {
            kind = alt,
            dir = (alt == 0 ? away : toward),
            dist = Mathf.Max(0.05f, alternateFull * Mathf.Clamp01(evadeReducedFactor))
        });
    }

    private void ComputeFullDistances(float dx, out float primaryFull, out float alternateFull)
    {
        float padding = 1f;

        float backFull = UnityEngine.Random.Range(
            Mathf.Min(evadeBackAwayMin, evadeBackAwayMax),
            Mathf.Max(evadeBackAwayMin, evadeBackAwayMax)
        );

        float passFull = dx + UnityEngine.Random.Range(
            Mathf.Min(evadePassPastMin, evadePassPastMax),
            Mathf.Max(evadePassPastMin, evadePassPastMax)
        );

        if (intent == Intent.MoveAndAttack)
        {
            if (attackMaxRange > 0f)
            {
                backFull = attackMaxRange + padding;
                passFull = dx + attackMaxRange + padding;
            }
            else
            {
                backFull += padding;
                passFull += padding;
            }
        }

        if (primaryKind == 0)
        {
            primaryFull = backFull;
            alternateFull = passFull;
        }
        else
        {
            primaryFull = passFull;
            alternateFull = backFull;
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

    private bool IsInAttackStopRange()
    {
        if (intent != Intent.MoveAndAttack)
            return false;

        if (attackMaxRange <= 0f || attackStopRange <= 0f)
            return false;

        var player = PlayerUtils.GetPlayerSafe();
        if (player == null)
            return false;

        float dx = Mathf.Abs(player.transform.position.x - enemy.transform.position.x);
        return dx >= attackStopRange;
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var src = enemy.transform;
        for (int i = 0; i < sounds.Count; i++) sounds[i].Play(src);
    }
}
