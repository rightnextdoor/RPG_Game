using System.Collections.Generic;
using UnityEngine;

public class DeadStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private static readonly List<StateSound> Empty = new();

    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    public DeadStateBase(
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

    public override void Enter()
    {
        base.Enter();

        PlayStateSounds(enterSounds);

        enemy.stats.MakeInvincible(true);

        HideHealthBar();
        StopDeathVisuals();
    }

    public override void Update()
    {
        base.Update();

        if (!enemy.stats.isDeadZone)
            enemy.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();
        PlayStateSounds(exitSounds);
    }

    protected virtual void HideHealthBar()
    {
        UI_HealthBar healthBar = enemy.GetComponentInChildren<UI_HealthBar>();
        if (healthBar != null)
            healthBar.HidHealthBar();
    }

    protected virtual void StopDeathVisuals()
    {
        ParticleSystem[] particles = enemy.GetComponentsInChildren<ParticleSystem>();

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void PlayStateSounds(List<StateSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return;
        var src = enemy.transform;
        for (int i = 0; i < sounds.Count; i++)
            sounds[i].Play(src);
    }
}