using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_PiranhaPlant : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }
    #region States
    public IdleStateBase<Enemy_PiranhaPlant> idleState { get; private set; }
    public BattleStateBase<Enemy_PiranhaPlant> battleState { get; private set; }
    public AttackStateBase<Enemy_PiranhaPlant> attackState { get; private set; }
    public DeadStateBase<Enemy_PiranhaPlant> deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        SetupStates();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    #region Setup
    private void SetupStates()
    {
        BuildStates();
        MapAbilityStates();

        cooldownSystem = new CooldownSystem();
        cooldownSystem.Setup(abilityMap, battleMinCooldown, battleMaxCooldown);

        abilityHub = new AbilityHub();
        abilityHub.BuildAbilityLists(abilityMap);
        abilityHub.Setup(cooldownSystem);

        battleState?.Configure(abilityHub, cooldownSystem, abilityMap, battlePreference);
    }

    private void BuildStates()
    {
        StateDetail idleDetail = null;
        StateDetail battleDetail = null;
        StateDetail attack1Detail = null;
        StateDetail deadDetail = null;

        AssignStateDetails(EnemyStateType.Idle, detail => idleDetail = detail);
        AssignStateDetails(EnemyStateType.Battle, detail => battleDetail = detail);
        AssignStateDetails(EnemyStateType.Attack, detail => attack1Detail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);

        if (idleDetail != null)
        {
            idleState = new IdleWithTargets(
                this,
                stateMachine,
                idleDetail.animBoolName,
                moveFactory: () => null,
                battleFactory: () => battleState,
                enterSounds: ToSoundList(idleDetail.enterSounds),
                exitSounds: ToSoundList(idleDetail.exitSounds)
            );
        }

        if (battleDetail != null)
        {
            battleState = new BattleStateBase<Enemy_PiranhaPlant>(
                this,
                stateMachine,
                battleDetail.animBoolName,
                idleState: () => idleState,
                nextState: () => idleState,
                enterSounds: ToSoundList(battleDetail.enterSounds),
                exitSounds: ToSoundList(battleDetail.exitSounds)
            );
        }

        if (attack1Detail != null)
        {
            attackState = new AttackStateBase<Enemy_PiranhaPlant>(
                this,
                stateMachine,
                attack1Detail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attack1Detail.enterSounds),
                exitSounds: ToSoundList(attack1Detail.exitSounds)
            );
        }

        if (deadDetail != null)
        {
            deadState = new DeadStateBase<Enemy_PiranhaPlant>(
                this,
                stateMachine,
                deadDetail.animBoolName,
                enterSounds: ToSoundList(deadDetail.enterSounds),
                exitSounds: ToSoundList(deadDetail.exitSounds)
            );
        }

    }

    protected override void MapAbilityStates()
    {
        base.MapAbilityStates();

        if (abilityMap == null || abilityMap.Count == 0)
            return;

        foreach (var entry in abilityMap.Values)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.animBoolName))
                continue;

            switch (entry.action)
            {
                case BattleAction.Attack:
                    if (attackState != null && attackState.animBoolName == entry.animBoolName)
                        entry.state = attackState;
                    break;

                case BattleAction.Evade:
                    break;

                case BattleAction.Jump:
                    break;

                case BattleAction.Stunned:
                    break;

                case BattleAction.Teleport:
                    break;
            }
        }
    }

    public override void StartBattle()
    {
        base.StartBattle();
        cooldownSystem?.ApplyStartCooldowns();
    }

    private List<StateSound> ToSoundList(StateSound[] sounds)
    {
        if (sounds == null || sounds.Length == 0)
            return null;

        return new List<StateSound>(sounds);
    }

    private sealed class IdleWithTargets : IdleStateBase<Enemy_PiranhaPlant>
    {
        private readonly System.Func<EnemyState> moveFactory;
        private readonly System.Func<EnemyState> battleFactory;

        public IdleWithTargets(
            Enemy_PiranhaPlant enemy,
            EnemyStateMachine sm,
            string animBool,
            System.Func<EnemyState> moveFactory,
            System.Func<EnemyState> battleFactory,
            List<StateSound> enterSounds = null,
            List<StateSound> exitSounds = null
        ) : base(enemy, sm, animBool, enterSounds, exitSounds)
        {
            this.moveFactory = moveFactory;
            this.battleFactory = battleFactory;
        }

        protected override EnemyState MoveState => moveFactory?.Invoke();
        protected override EnemyState BattleState => battleFactory?.Invoke();
    }

    public override void ChangeBattleCooldownRange(float minCooldown, float maxCooldown)
    {
        cooldownSystem?.ChangeBattleCooldownRange(minCooldown, maxCooldown);
    }

    public override void TempChangeBattleCooldownRange(float minCooldown, float maxCooldown, float duration)
    {
        cooldownSystem?.TempChangeBattleCooldownRange(minCooldown, maxCooldown, duration);
    }

    public override void PauseBattleCooldown(bool pause)
    {
        cooldownSystem?.PauseBattleCooldown(pause);
    }

    #endregion

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }
}
