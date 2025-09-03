using System;
using System.Collections.Generic;
using UnityEngine;

public class EvasionStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    public enum Intent { MoveAndAttack, Flee }

    private readonly Func<EnemyState> defaultNextState;
    private Func<EnemyState> nextState;       
    private Func<EnemyState> attackState;    
    private float attackRange;                
    private Intent intent;

    private int moveDir;
    private bool configured;

    private static readonly List<StateSound> _empty = new List<StateSound>();
    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

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

    // called by battle
    public void ConfigureMoveAndAttack(Func<EnemyState> attack, float range, Func<EnemyState> after = null)
    {
        intent = Intent.MoveAndAttack;
        attackState = attack;
        attackRange = range;
        nextState = after ?? defaultNextState;
        configured = true;
    }

    // called by battle
    public void ConfigureFlee(Func<EnemyState> after = null)
    {
        intent = Intent.Flee;
        nextState = after ?? defaultNextState;
        configured = true;
    }

    public override void Enter()
    {
        base.Enter();

        if (!configured)
        {
            intent = Intent.Flee;
            nextState = defaultNextState;
        }

        stateTimer = enemy.evasionDuration;
        enemy.MarkEvaded();

        moveDir = DecideInitialDirection();

        PlayStateSounds(enterSounds);
    }

    public override void Update()
    {
        base.Update();

        switch (intent)
        {
            case Intent.MoveAndAttack:
                MoveAndAttack();
                break;
            case Intent.Flee:
                Flee();
                break;
        }

        if (stateTimer <= 0f)
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(nextState());
        }
    }

    public override void Exit()
    {
        base.Exit();
        enemy.SetZeroVelocity();
        PlayStateSounds(exitSounds);
        configured = false;
    }

    protected virtual void MoveAndAttack()
    {
        float speed = enemy.moveSpeed * enemy.evasionSpeedMultiplier * moveDir;
        enemy.SetVelocity(speed, rb.velocity.y);

        if (!HasForwardClearance(moveDir))
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(defaultNextState());
            return;
        }

        var player = PlayerUtils.GetPlayerSafe();
        if (player != null)
        {
            float dx = Mathf.Abs(player.transform.position.x - enemy.transform.position.x);
            if (dx <= attackRange)
            {
                if ((player.transform.position.x - enemy.transform.position.x) * enemy.facingDir < 0)
                    enemy.Flip();

                stateMachine.ChangeState(attackState());
            }
        }
    }

    protected virtual void Flee()
    {
        float speed = enemy.moveSpeed * enemy.evasionSpeedMultiplier * moveDir;
        enemy.SetVelocity(speed, rb.velocity.y);

        if (!HasForwardClearance(moveDir))
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(defaultNextState());
        }
    }

    private int DecideInitialDirection()
    {
        int playerDir = GetPlayerDirX(); 
        bool tryPassThrough = UnityEngine.Random.value < 0.5f;

        if (tryPassThrough)
        {
            int through = (playerDir == 0) ? enemy.facingDir : playerDir; 
            if (HasForwardClearance(through)) return through;

            int away = -through;
            if (HasForwardClearance(away)) return away;

            return enemy.facingDir;
        }
        else
        {
            int away = (playerDir == 0) ? -enemy.facingDir : -playerDir; 
            if (HasForwardClearance(away)) return away;

            int through = -away;
            if (HasForwardClearance(through)) return through;

            return enemy.facingDir;
        }
    }


    private int GetPlayerDirX()
    {
        var player = PlayerUtils.GetPlayerSafe();
        if (player == null) return 0;
        float dx = player.transform.position.x - enemy.transform.position.x;
        if (Mathf.Abs(dx) < 0.001f) return 0;
        return dx > 0f ? 1 : -1;
    }

    private bool HasForwardClearance(int dir)
    {
        if (dir == 0) return true;

        float distance = Mathf.Max(0.01f,
            enemy.moveSpeed * enemy.evasionSpeedMultiplier * Mathf.Max(0.05f, enemy.evasionDuration));

        Vector2 origin = enemy.GetWallCheck() != null
            ? (Vector2)enemy.GetWallCheck().position
            : (Vector2)enemy.transform.position;
        Vector2 ahead = Vector2.right * dir;
        bool wallHit = Physics2D.Raycast(origin, ahead, distance, enemy.GetWhatIsGround());
#if UNITY_EDITOR
        Debug.DrawLine(origin, origin + ahead * distance, wallHit ? Color.red : Color.green, 0f);
#endif
        if (wallHit) return false;

        Vector2 edgeProbeStart = origin + ahead * distance;
        float down = Mathf.Max(0.1f, enemy.GetGroundCheackDistance() * 1.1f);
        bool groundThere = Physics2D.Raycast(edgeProbeStart, Vector2.down, down, enemy.GetWhatIsGround());
#if UNITY_EDITOR
        Debug.DrawLine(edgeProbeStart, edgeProbeStart + Vector2.down * down, groundThere ? Color.green : Color.red, 0f);
#endif
        return groundThere;
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var src = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(src);
    }
}
