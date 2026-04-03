using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Knight_Archer : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }

    #region States
    public IdleStateBase<Enemy_Knight_Archer> idleState { get; private set; }
    public IdleStateBase<Enemy_Knight_Archer> idle2State { get; private set; }
    public MoveStateBase<Enemy_Knight_Archer> moveState { get; private set; }
    public BattleStateBase<Enemy_Knight_Archer> battleState { get; private set; }
    public AttackStateBase<Enemy_Knight_Archer> attackState { get; private set; }
    public AttackStateBase<Enemy_Knight_Archer> attack2State { get; private set; }
    public DeadStateBase<Enemy_Knight_Archer> deadState { get; private set; }
    public EvasionStateBase<Enemy_Knight_Archer> evasionState { get; private set; }

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
        StateDetail idle2Detail = null;
        StateDetail moveDetail = null;
        StateDetail battleDetail = null;
        StateDetail attackDetail = null;
        StateDetail attack2Detail = null;
        StateDetail deadDetail = null;
        StateDetail evasionDetail = null;

        AssignStateDetails(EnemyStateType.Idle, 
            detail => idleDetail = detail,
            detail => idle2Detail = detail);
        AssignStateDetails(EnemyStateType.Move, detail => moveDetail = detail);
        AssignStateDetails(EnemyStateType.Battle, detail => battleDetail = detail);
        AssignStateDetails(EnemyStateType.Attack,
            detail => attackDetail = detail,
            detail => attack2Detail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);
        AssignStateDetails(EnemyStateType.Evasion, detail => evasionDetail = detail);

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
        if (idle2Detail != null)
        {
            idle2State = new IdleWithTargets(
                this,
                stateMachine,
                idle2Detail.animBoolName,
                moveFactory: () => moveState,
                battleFactory: () => battleState,
                enterSounds: ToSoundList(idle2Detail.enterSounds),
                exitSounds: ToSoundList(idle2Detail.exitSounds)
            );
        }

        if (moveDetail != null)
        {
            moveState = new MoveStateBase<Enemy_Knight_Archer>(
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
            battleState = new BattleStateBase<Enemy_Knight_Archer>(
                this,
                stateMachine,
                battleDetail.animBoolName,
                idleState: () => idle2State,
                nextState: () => idleState,
                enterSounds: ToSoundList(battleDetail.enterSounds),
                exitSounds: ToSoundList(battleDetail.exitSounds)
            );
        }

        if (attackDetail != null)
        {
            attackState = new AttackStateBase<Enemy_Knight_Archer>(
                this,
                stateMachine,
                attackDetail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attackDetail.enterSounds),
                exitSounds: ToSoundList(attackDetail.exitSounds)
            );
        }

        if (attack2Detail != null)
        {
            attack2State = new AttackStateBase<Enemy_Knight_Archer>(
                this,
                stateMachine,
                attack2Detail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attack2Detail.enterSounds),
                exitSounds: ToSoundList(attack2Detail.exitSounds)
            );
        }

        if (deadDetail != null)
        {
            deadState = new DeadStateBase<Enemy_Knight_Archer>(
                this,
                stateMachine,
                deadDetail.animBoolName,
                enterSounds: ToSoundList(deadDetail.enterSounds),
                exitSounds: ToSoundList(deadDetail.exitSounds)
            );
        }

        if (evasionDetail != null)
        {
            evasionState = new EvasionStateBase<Enemy_Knight_Archer>(
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
            if (entry == null || string.IsNullOrWhiteSpace(entry.animBoolName))
                continue;

            switch (entry.action)
            {
                case BattleAction.Attack:
                    if (attackState != null && attackState.animBoolName == entry.animBoolName)
                        entry.state = attackState;
                    else if (attack2State != null && attack2State.animBoolName == entry.animBoolName)
                        entry.state = attack2State;
                    break;

                case BattleAction.Evade:
                    if (evasionState != null && evasionState.animBoolName == entry.animBoolName)
                        entry.state = evasionState;
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

    private sealed class IdleWithTargets : IdleStateBase<Enemy_Knight_Archer>
    {
        private readonly System.Func<EnemyState> moveFactory;
        private readonly System.Func<EnemyState> battleFactory;

        public IdleWithTargets(
            Enemy_Knight_Archer enemy,
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
        if (stats.isDeadZone)
        {
            deadState = new DeadStateBase<Enemy_Knight_Archer>(this, stateMachine, "Idle");
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

}
