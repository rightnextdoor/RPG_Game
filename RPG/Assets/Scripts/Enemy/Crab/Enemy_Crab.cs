using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Crab : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }
    #region States
    public IdleStateBase<Enemy_Crab> idleState { get; private set; }
    public CrabMoveState moveState { get; private set; }
    public DeadStateBase<Enemy_Crab> deadState { get; private set; }
    public RunIntoPlayerStateBase<Enemy_Crab> runIntoState { get; private set; }
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

        moveState?.Configure(abilityHub, cooldownSystem);
    }

    private void BuildStates()
    {
        StateDetail idleDetail = null;
        StateDetail moveDetail = null;
        StateDetail deadDetail = null;
        StateDetail runIntoDetail = null;

        AssignStateDetails(EnemyStateType.Idle, detail => idleDetail = detail);
        AssignStateDetails(EnemyStateType.Move, detail => moveDetail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);
        AssignStateDetails(EnemyStateType.RunIntoPlayer, detail => runIntoDetail = detail);

        if (idleDetail != null)
        {
            idleState = new IdleStateBase<Enemy_Crab>(
                this,
                stateMachine,
                idleDetail.animBoolName,
                moveState: () => moveState,
                battleState: () => null,
                enterSounds: ToSoundList(idleDetail.enterSounds),
                exitSounds: ToSoundList(idleDetail.exitSounds)
            );
        }

        if (moveDetail != null)
        {
            moveState = new CrabMoveState(
                this,
                stateMachine,
                moveDetail.animBoolName,
                idleState: () => idleState,
                battleState: () => runIntoState,
                enterSounds: ToSoundList(moveDetail.enterSounds),
                exitSounds: ToSoundList(moveDetail.exitSounds)
            );
        }

        if (runIntoDetail != null)
        {
            runIntoState = new RunIntoPlayerStateBase<Enemy_Crab>(
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
            deadState = new DeadStateBase<Enemy_Crab>(
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

    #endregion

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }
}
