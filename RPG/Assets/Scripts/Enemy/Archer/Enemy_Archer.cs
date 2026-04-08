using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Archer : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }

    #region States
    public IdleStateBase<Enemy_Archer> idleState { get; private set; }
    public MoveStateBase<Enemy_Archer> moveState { get; private set; }
    public BattleStateBase<Enemy_Archer> battleState { get; private set; }
    public AttackStateBase<Enemy_Archer> attackState { get; private set; }
    public StunnedStateBase<Enemy_Archer> stunnedState { get; private set; }
    public DeadStateBase<Enemy_Archer> deadState { get; private set; }
    public JumpStateBase<Enemy_Archer> jumpState { get; private set; }
    public EvasionStateBase<Enemy_Archer> evasionState { get; private set; }
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
        StateDetail moveDetail = null;
        StateDetail battleDetail = null;
        StateDetail attackDetail = null;
        StateDetail stunnedDetail = null;
        StateDetail deadDetail = null;
        StateDetail jumpDetail = null;
        StateDetail evasionDetail = null;

        AssignStateDetails(EnemyStateType.Idle, detail => idleDetail = detail);
        AssignStateDetails(EnemyStateType.Move, detail => moveDetail = detail);
        AssignStateDetails(EnemyStateType.Battle, detail => battleDetail = detail);
        AssignStateDetails(EnemyStateType.Attack, detail => attackDetail = detail);
        AssignStateDetails(EnemyStateType.Stunned, detail => stunnedDetail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);
        AssignStateDetails(EnemyStateType.Jump, detail => jumpDetail = detail);
        AssignStateDetails(EnemyStateType.Evasion, detail => evasionDetail = detail);

        if (idleDetail != null)
        {
            idleState = new IdleStateBase<Enemy_Archer>(
                this,
                stateMachine,
                idleDetail.animBoolName,
                moveState: () => moveState,
                battleState: () => battleState,
                enterSounds: ToSoundList(idleDetail.enterSounds),
                exitSounds: ToSoundList(idleDetail.exitSounds)
            );
        }

        if (moveDetail != null)
        {
            moveState = new MoveStateBase<Enemy_Archer>(
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
            battleState = new BattleStateBase<Enemy_Archer>(
                this,
                stateMachine,
                battleDetail.animBoolName,
                idleState: () => idleState,
                nextState: () => idleState,
                enterSounds: ToSoundList(battleDetail.enterSounds),
                exitSounds: ToSoundList(battleDetail.exitSounds)
            );
        }

        if (attackDetail != null)
        {
            attackState = new AttackStateBase<Enemy_Archer>(
                this,
                stateMachine,
                attackDetail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attackDetail.enterSounds),
                exitSounds: ToSoundList(attackDetail.exitSounds)
            );
        }

        if (stunnedDetail != null)
        {
            stunnedState = new StunnedStateBase<Enemy_Archer>(
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
            deadState = new DeadStateBase<Enemy_Archer>(
                this,
                stateMachine,
                deadDetail.animBoolName,
                enterSounds: ToSoundList(deadDetail.enterSounds),
                exitSounds: ToSoundList(deadDetail.exitSounds)
            );
        }

        if (jumpDetail != null)
        {
            jumpState = new JumpStateBase<Enemy_Archer>(
                this,
                stateMachine,
                jumpDetail.animBoolName,
                enterSounds: ToSoundList(jumpDetail.enterSounds),
                exitSounds: ToSoundList(jumpDetail.exitSounds)
            );
        }

        if (evasionDetail != null)
        {
            evasionState = new EvasionStateBase<Enemy_Archer>(
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
                    break;

                case BattleAction.Evade:
                    if (evasionState != null && evasionState.animBoolName == entry.animBoolName)
                        entry.state = evasionState;
                    break;

                case BattleAction.Jump:
                    if (jumpState != null && jumpState.animBoolName == entry.animBoolName)
                        entry.state = jumpState;
                    break;

                case BattleAction.Stunned:
                    if (stunnedState != null && stunnedState.animBoolName == entry.animBoolName)
                        entry.state = stunnedState;
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
            deadState = new DeadStateBase<Enemy_Archer>(this, stateMachine, "Idle");
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

}
