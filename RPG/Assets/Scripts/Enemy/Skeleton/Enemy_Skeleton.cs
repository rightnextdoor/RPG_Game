using System.Collections.Generic;
using UnityEngine;

public class Enemy_Skeleton : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }
    #region States
    public IdleStateBase<Enemy_Skeleton> idleState { get; private set; }
    public MoveStateBase<Enemy_Skeleton> moveState { get; private set; }
    public BattleStateBase<Enemy_Skeleton> battleState { get; private set; }
    public AttackStateBase<Enemy_Skeleton> attackState { get; private set; }
    public AttackStateBase<Enemy_Skeleton> attack2State { get; private set; }
    public StunnedStateBase<Enemy_Skeleton> stunnedState { get; private set; }
    public DeadStateBase<Enemy_Skeleton> deadState { get; private set; }
    public EvasionStateBase<Enemy_Skeleton> evasionState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        SetupStates();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

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
        StateDetail idleDetail = GetStateDetail(EnemyStateType.Idle);
        StateDetail moveDetail = GetStateDetail(EnemyStateType.Move);
        StateDetail battleDetail = GetStateDetail(EnemyStateType.Battle);
        StateDetail attack1Detail = GetStateDetailByName("Attack1");
        StateDetail attack2Detail = GetStateDetailByName("Attack2");
        StateDetail stunnedDetail = GetStateDetail(EnemyStateType.Stunned);
        StateDetail deadDetail = GetStateDetail(EnemyStateType.Dead);
        StateDetail evasionDetail = GetStateDetail(EnemyStateType.Evasion);

        if (idleDetail != null)
        {
            idleState = new IdleWithTargets(
                this,
                stateMachine,
                idleDetail.animBoolName,
                moveFactory: () => moveState,
                battleFactory: () => battleState,
                enterSounds: ToSoundList(idleDetail.enterSounds),
                exitSounds: ToSoundList(idleDetail.exitSounds)
            );
        }

        if (moveDetail != null)
        {
            moveState = new MoveStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                moveDetail.animBoolName,
                idleState: () => idleState,
                battleState: () => battleState,
                enterSounds: ToSoundList(moveDetail.enterSounds),
                exitSounds: ToSoundList(moveDetail.exitSounds)
            );
        }

        if (battleDetail != null)
        {
            battleState = new BattleStateBase<Enemy_Skeleton>(
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
            attackState = new AttackStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                attack1Detail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attack1Detail.enterSounds),
                exitSounds: ToSoundList(attack1Detail.exitSounds)
            );
        }

        if (attack2Detail != null)
        {
            attack2State = new AttackStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                attack2Detail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attack2Detail.enterSounds),
                exitSounds: ToSoundList(attack2Detail.exitSounds)
            );
        }

        if (stunnedDetail != null)
        {
            stunnedState = new StunnedStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                stunnedDetail.animBoolName,
                nextState: () => battleState,
                enterSounds: ToSoundList(stunnedDetail.enterSounds),
                exitSounds: ToSoundList(stunnedDetail.exitSounds)
            );
        }

        if (deadDetail != null)
        {
            deadState = new DeadStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                deadDetail.animBoolName,
                enterSounds: ToSoundList(deadDetail.enterSounds),
                exitSounds: ToSoundList(deadDetail.exitSounds)
            );
        }

        if (evasionDetail != null)
        {
            evasionState = new EvasionStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                evasionDetail.animBoolName,
                battleState: () => battleState,
                enterSounds: ToSoundList(evasionDetail.enterSounds),
                exitSounds: ToSoundList(evasionDetail.exitSounds)
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
            if (entry == null)
                continue;

            switch (entry.action)
            {
                case BattleAction.Attack:
                    if (entry.stateName == "Attack1" && attackState != null)
                        entry.state = attackState;
                    else if (entry.stateName == "Attack2" && attack2State != null)
                        entry.state = attack2State;
                    else if (attackState != null)
                        entry.state = attackState;
                    break;

                case BattleAction.Evade:
                    if (evasionState != null)
                        entry.state = evasionState;
                    break;

                case BattleAction.Jump:
                    break;

                case BattleAction.Stunned:
                    if (stunnedState != null)
                    {
                        entry.state = stunnedState;
                        stunnedState.Configure(entry.stunDuration, entry.stunDirection);
                    }
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

    private sealed class IdleWithTargets : IdleStateBase<Enemy_Skeleton>
    {
        private readonly System.Func<EnemyState> moveFactory;
        private readonly System.Func<EnemyState> battleFactory;

        public IdleWithTargets(
            Enemy_Skeleton enemy,
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


    public override bool CanBeStunned()
    {
        if (base.CanBeStunned())
        {
            stateMachine.ChangeState(stunnedState);
            return true;
        }
        return false;
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new DeadStateBase<Enemy_Skeleton>(this, stateMachine, "Idle");
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void SelfDestroy()
    {
        base.SelfDestroy();

        if (GetComponentInChildren<UI_HealthBar>() != null)
        {
            GetComponentInChildren<UI_HealthBar>().HidHealthBar();
        }

        Destroy(gameObject, 2f);
    }

}
