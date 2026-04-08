using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Enemy_Slug : Enemy_Regular
{
    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }
    #region States
    public SlugMoveState moveState { get; private set; }
    public DeadStateBase<Enemy_Slug> deadState { get; private set; }
    public RunIntoPlayerStateBase<Enemy_Slug> runIntoState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        SetupStates();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(moveState);
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
        StateDetail moveDetail = null;
        StateDetail deadDetail = null;
        StateDetail runIntoDetail = null;

        AssignStateDetails(EnemyStateType.Move, detail => moveDetail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);
        AssignStateDetails(EnemyStateType.RunIntoPlayer, detail => runIntoDetail = detail);

        if (moveDetail != null)
        {
            moveState = new SlugMoveState(
                this,
                stateMachine,
                moveDetail.animBoolName,
                idleState: () => moveState,
                battleState: () => runIntoState,
                enterSounds: ToSoundList(moveDetail.enterSounds),
                exitSounds: ToSoundList(moveDetail.exitSounds)
            );
        }

        if (runIntoDetail != null)
        {
            runIntoState = new RunIntoPlayerStateBase<Enemy_Slug>(
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
            deadState = new DeadStateBase<Enemy_Slug>(
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
