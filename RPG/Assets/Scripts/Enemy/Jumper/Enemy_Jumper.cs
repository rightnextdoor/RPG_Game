using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Jumper : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }

    #region States
    public BounceIdleStateBase<Enemy_Jumper> idleState { get; private set; }
    public BounceMoveStateBase<Enemy_Jumper> moveState { get; private set; }
    public BounceBattleStateBase<Enemy_Jumper> battleState { get; private set; }
    public DeadStateBase<Enemy_Jumper> deadState { get; private set; }
    public RunIntoPlayerStateBase<Enemy_Jumper> runIntoState { get; private set; }
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

    }

    private void BuildStates()
    {
        StateDetail idleDetail = null;
        StateDetail moveDetail = null;
        StateDetail battleDetail = null;
        StateDetail deadDetail = null;
        StateDetail runIntoDetail = null;

        AssignStateDetails(EnemyStateType.BounceIdle, detail => idleDetail = detail);
        AssignStateDetails(EnemyStateType.BounceMove, detail => moveDetail = detail);
        AssignStateDetails(EnemyStateType.BounceBattle, detail => battleDetail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);
        AssignStateDetails(EnemyStateType.RunIntoPlayer, detail => runIntoDetail = detail);

        if (idleDetail != null)
        {
            idleState = new BounceIdleStateBase<Enemy_Jumper>(
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
            moveState = new BounceMoveStateBase<Enemy_Jumper>(
                this,
                stateMachine,
                moveDetail.animBoolName,
                idleState: () => moveState,
                battleState: () => runIntoState,
                enterSounds: ToSoundList(moveDetail.enterSounds),
                exitSounds: ToSoundList(moveDetail.exitSounds)
            );
        }

        if (battleDetail != null)
        {
            battleState = new BounceBattleStateBase<Enemy_Jumper>(
                this,
                stateMachine,
                battleDetail.animBoolName,
                idleState: () => idleState,
                moveState: () => moveState,
                runIntoState: () => runIntoState,
                enterSounds: ToSoundList(battleDetail.enterSounds),
                exitSounds: ToSoundList(battleDetail.exitSounds)
            );
        }

        if (runIntoDetail != null)
        {
            runIntoState = new RunIntoPlayerStateBase<Enemy_Jumper>(
                this,
                stateMachine,
                runIntoDetail.animBoolName,
                nextState: () => moveState,
                enterSounds: ToSoundList(moveDetail.enterSounds),
                exitSounds: ToSoundList(moveDetail.exitSounds)
                );
        }

        if (deadDetail != null)
        {
            deadState = new DeadStateBase<Enemy_Jumper>(
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
                    break;

                case BattleAction.Evade:
                    break;

                case BattleAction.Jump:
                    break;

                case BattleAction.Stunned:

                    break;

                case BattleAction.Teleport:
                    break;

                case BattleAction.RunIntoPlayer:
                    if (runIntoState != null && runIntoState.animBoolName == entry.animBoolName)
                        entry.state = runIntoState;
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

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }
  
}
