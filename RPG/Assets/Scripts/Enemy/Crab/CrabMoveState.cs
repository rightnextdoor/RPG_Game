using System;
using System.Collections.Generic;
using UnityEngine;

public class CrabMoveState : MoveStateBase<Enemy_Crab>
{
    protected AbilityHub hub;
    protected CooldownSystem cooldownSystem;

    public CrabMoveState(
        Enemy_Crab enemyBase,
        EnemyStateMachine stateMachine,
        string animBoolName,
        Func<EnemyState> idleState,
        Func<EnemyState> battleState,
        List<StateSound> enterSounds = null,
        List<StateSound> exitSounds = null
    ) : base(
        enemyBase,
        stateMachine,
        animBoolName,
        idleState,
        battleState,
        enterSounds,
        exitSounds)
    {
    }

    #region Configure
    public virtual void Configure(AbilityHub abilityHub, CooldownSystem enemyCooldownSystem)
    {
        hub = abilityHub;
        cooldownSystem = enemyCooldownSystem;
    }
    #endregion

    #region State
    public override void Enter()
    {
        base.Enter();

        if (!enemy.BattleStarted)
            enemy.StartBattle();
    }
    #endregion

    #region Slug battle hook
    protected override bool TryChangeToBattleState()
    {
        if (hub == null || cooldownSystem == null)
            return false;

        AbilityEntry entry = TryGetRunIntoAbility();
        if (entry == null)
            return false;

        enemy.CurrentAbilityEntry = entry;

        if (enemy.runIntoState != null)
            enemy.runIntoState.Configure(RunIntoPlayerMode.Hit, () => enemy.moveState);

        cooldownSystem.SetAbilityCooldown(entry.action, entry.name);
        stateMachine.ChangeState(enemy.runIntoState);

        return true;
    }

    protected virtual AbilityEntry TryGetRunIntoAbility()
    {
        AbilityEntry entry = hub.GetAbility(BattleAction.RunIntoPlayer, AbilityPreference.Short);
        if (entry == null)
            return null;

        if (!IsPlayerInRunIntoRange(entry))
            return null;

        return entry;
    }

    protected virtual bool IsPlayerInRunIntoRange(AbilityEntry entry)
    {
        if (entry == null)
            return false;

        var player = PlayerUtils.GetPlayerSafe();
        if (player == null)
            return false;

        float playerDistance = Vector2.Distance(enemy.transform.position, player.transform.position);

        float maxRange = entry.rangeMax ?? 0f;
        if (maxRange <= 0f)
            maxRange = 1.5f;

        return playerDistance <= maxRange;
    }
    #endregion
}
