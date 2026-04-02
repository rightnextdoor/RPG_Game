using System;
using System.Collections.Generic;
using UnityEngine;

public class TeleportStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private static readonly List<StateSound> Empty = new();

    private Func<EnemyState> nextProvider;
    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    public TeleportStateBase(
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

    public virtual void Configure(Func<EnemyState> next)
    {
        nextProvider = next;
    }

    public virtual void SetNextState(Func<EnemyState> next)
    {
        nextProvider = next;
    }

    public override void Enter()
    {
        base.Enter();
        PlayStateSounds(enterSounds);
    }

    public override void Update()
    {
        base.Update();

        if (triggerCalled)
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

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var xf = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(xf);
    }
}
