using System;
using System.Collections.Generic;
using UnityEngine;

public class StunnedStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    private static readonly List<StateSound> Empty = new();

    private readonly Func<EnemyState> nextState; 
    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private float stunDuration;
    private Vector2 stunDirection;

    public StunnedStateBase(
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

    public void Configure(float stunDuration, Vector2 stunDirection)
    {
        this.stunDuration = stunDuration;
        this.stunDirection = stunDirection;
    }

    public override void Enter()
    {
        base.Enter();

        PlayStateSounds(enterSounds);

        enemy.fX.InvokeRepeating("RedColorBlink", 0f, 0.1f);
        stateTimer = stunDuration;

        rb.linearVelocity = new Vector2(-enemy.facingDir * stunDirection.x, stunDirection.y);
    }

    public override void Exit()
    {
        base.Exit();

        enemy.fX.Invoke("CancelColorChange", 0f);

        PlayStateSounds(exitSounds);
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0f && nextState != null)
            stateMachine.ChangeState(nextState());
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var src = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(src);
    }
}
