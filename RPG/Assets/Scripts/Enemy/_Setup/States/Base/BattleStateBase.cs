using System.Collections.Generic;
using UnityEngine;

public class BattleStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    #region Settings
    private readonly System.Func<EnemyState> idleState;
    private readonly System.Func<EnemyState> nextState;

    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private Transform player;

    private AbilityHub hub;
    private CooldownSystem cooldownSystem;

    private AbilityPreference preferredAttackPreference = AbilityPreference.Mixed;
    private BattleAction currentPlanAction = BattleAction.None;

    private readonly List<AbilityEntry> attackAbilities = new();
    private readonly List<AbilityEntry> evadeAbilities = new();
    private readonly List<AbilityEntry> jumpAbilities = new();
    private readonly List<AbilityEntry> teleportAbilities = new();

    private float shortAttackMinRange = 0f;
    private float shortAttackMaxRange = 0f;

    private float longAttackMinRange = 0f;
    private float longAttackMaxRange = 0f;

    private float mixedAttackMinRange = 0f;
    private float mixedAttackMaxRange = 0f;

    private float preferredRangeMin = 0f;
    private float preferredRangeMax = 0f;

    private bool hasTeleport = false;
    private bool hasJump = false;
    private bool hasEvade = false;

    private float jumpMaxRange = 0f;
    private float evadeMaxRange = 0f;

    private AbilityEntry selectedAttackEntry;
    #endregion

    #region Configure
    public BattleStateBase(
       TEnemy enemyBase,
       EnemyStateMachine stateMachine,
       string animBoolName,
       System.Func<EnemyState> idleState,
       System.Func<EnemyState> nextState,
       List<StateSound> enterSounds = null,
       List<StateSound> exitSounds = null
   ) : base(enemyBase, stateMachine, animBoolName)
    {
        this.idleState = idleState;
        this.nextState = nextState;

        this.enterSounds = enterSounds ?? new List<StateSound>();
        this.exitSounds = exitSounds ?? new List<StateSound>();
    }

    public void Configure(
    AbilityHub abilityHub,
    CooldownSystem enemyCooldownSystem,
    Dictionary<string, AbilityEntry> abilityMap,
    AbilityPreference enemyPreferredAttackPreference)
    {
        hub = abilityHub;
        cooldownSystem = enemyCooldownSystem;

        preferredAttackPreference = enemyPreferredAttackPreference;

        BuildAbilityLists(abilityMap);
    }

    #endregion

    #region State 
    public override void Enter()
    {
        base.Enter();

        if (!enemy.BattleStarted)
        {
            enemy.StartBattle();
            SetupBattleData();
        }

        enemy.CurrentAbilityEntry = null;

        player = PlayerUtils.GetPlayerSafe().transform;

        if (player.GetComponent<PlayerStats>().isDead && nextState != null)
        {
            stateMachine.ChangeState(nextState());
            return;
        }

        stateTimer = enemy.battleTime;
        FlipTowardsPlayer();
    }

    public override void Update()
    {
        base.Update();

        UpdateAnim();

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            if (enemy.IsPlayerDetected())
            {
                PlayerDetected();
            }
            else
            {
                if (idleState != null) stateMachine.ChangeState(idleState());
            }
            return;
        }

        PlayerDetected();
    }

    public override void Exit()
    {
        base.Exit();
    }

    protected virtual void UpdateAnim()
    {
        if (enemy.anim != null && enemy.rb != null)
            enemy.anim.SetFloat("xVelocity", enemy.rb.linearVelocity.x);
    }
    #endregion

    #region Setup
    private void SetupBattleData()
    {
        SetupAttackRangeInfo();
        SetupPreferredRange();
        SetupSupportMaxRanges();
        SetupAbilityFlags();
    }

    private void SetupAttackRangeInfo()
    {
        shortAttackMinRange = 0f;
        shortAttackMaxRange = 0f;

        longAttackMinRange = 0f;
        longAttackMaxRange = 0f;

        mixedAttackMinRange = 0f;
        mixedAttackMaxRange = 0f;

        if (attackAbilities == null || attackAbilities.Count == 0)
            return;

        foreach (AbilityEntry entry in attackAbilities)
        {
            if (entry == null) continue;

            float rangeMin = entry.rangeMin ?? 0f;
            float rangeMax = entry.rangeMax ?? 0f;

            switch (entry.preference)
            {
                case AbilityPreference.Short:
                    if (rangeMin > shortAttackMinRange)
                        shortAttackMinRange = rangeMin;

                    if (rangeMax > shortAttackMaxRange)
                        shortAttackMaxRange = rangeMax;
                    break;

                case AbilityPreference.Long:
                    if (rangeMin > longAttackMinRange)
                        longAttackMinRange = rangeMin;

                    if (rangeMax > longAttackMaxRange)
                        longAttackMaxRange = rangeMax;
                    break;
            }
        }

        if (shortAttackMinRange <= 0f && shortAttackMaxRange > 0f)
            shortAttackMinRange = shortAttackMaxRange * 0.5f;

        if (longAttackMinRange <= 0f && longAttackMaxRange > 0f)
            longAttackMinRange = longAttackMaxRange * 0.5f;

        mixedAttackMinRange = shortAttackMaxRange;
        mixedAttackMaxRange = longAttackMinRange;
    }

    private void SetupSupportMaxRanges()
    {
        jumpMaxRange = 0f;
        evadeMaxRange = 0f;

        if (jumpAbilities != null)
        {
            foreach (AbilityEntry entry in jumpAbilities)
            {
                if (entry == null) continue;

                float rangeMax = entry.rangeMax ?? 0f;

                if (rangeMax > jumpMaxRange)
                    jumpMaxRange = rangeMax;
            }
        }

        if (evadeAbilities != null)
        {
            foreach (AbilityEntry entry in evadeAbilities)
            {
                if (entry == null) continue;

                float rangeMax = entry.rangeMax ?? 0f;

                if (rangeMax > evadeMaxRange)
                    evadeMaxRange = rangeMax;
            }
        }
    }

    private void SetupPreferredRange()
    {
        preferredRangeMin = 0f;
        preferredRangeMax = 0f;

        switch (preferredAttackPreference)
        {
            case AbilityPreference.Short:
                preferredRangeMin = shortAttackMinRange;
                preferredRangeMax = shortAttackMaxRange;
                break;

            case AbilityPreference.Long:
                preferredRangeMin = longAttackMinRange;
                preferredRangeMax = longAttackMaxRange;
                break;

            case AbilityPreference.Mixed:
                preferredRangeMin = mixedAttackMinRange;
                preferredRangeMax = mixedAttackMaxRange;
                break;
        }
    }

    private void SetupAbilityFlags()
    {
        hasTeleport = false;
        hasJump = false;
        hasEvade = false;

        if (teleportAbilities != null)
        {
            foreach (AbilityEntry entry in teleportAbilities)
            {
                if (entry == null) continue;
                if (entry.state == null) continue;

                hasTeleport = true;
                break;
            }
        }

        if (jumpAbilities != null)
        {
            foreach (AbilityEntry entry in jumpAbilities)
            {
                if (entry == null) continue;
                if (entry.state == null) continue;

                hasJump = true;
                break;
            }
        }

        if (evadeAbilities != null)
        {
            foreach (AbilityEntry entry in evadeAbilities)
            {
                if (entry == null) continue;
                if (entry.state == null) continue;

                hasEvade = true;
                break;
            }
        }
    }

    #endregion

    #region Player detection
    private void PlayerDetected()
    {
        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;

            if (cooldownSystem != null && cooldownSystem.IsBattleCooldownReady())
            {
                DecideFromHub();
            }
            else
            {
                UpdateBattleSpacingOnly(GetPlayerDistance());
            }
        }
        else
        {
            if (stateTimer < 0f ||
                Vector2.Distance(player.position, enemy.transform.position) > enemy.playerDistance)
            {
                enemy.EndBattle();

                if (idleState != null)
                    stateMachine.ChangeState(idleState());
            }
        }
    }

    private void FlipTowardsPlayer()
    {
        if (player == null) return;

        float dx = player.position.x - enemy.transform.position.x;
        if (dx > 0f && enemy.facingDir == -1) enemy.Flip();
        else if (dx < 0f && enemy.facingDir == 1) enemy.Flip();
    }

    #endregion

    #region Hub

    protected virtual void DecideFromHub()
    {
        if (cooldownSystem == null || !cooldownSystem.IsBattleCooldownReady()) return;
        if (hub == null) return;

        float playerDistance = GetPlayerDistance();
        AbilityPreference requestPreference = GetAttackPreferenceFromDistance(playerDistance);

        selectedAttackEntry = hub.GetAbility(BattleAction.Attack, requestPreference);

        float updatedPlayerDistance = GetPlayerDistance();
        RunBattlePlanSwitch(updatedPlayerDistance);
    }
    #endregion

    #region Battle decision

    private void RunBattlePlanSwitch(float playerDistance)
    {
        switch (currentPlanAction)
        {
            case BattleAction.Teleport:
                RunTeleportPlan(playerDistance);
                break;

            case BattleAction.Jump:
                RunJumpPlan(playerDistance);
                break;

            case BattleAction.Evade:
                RunEvadePlan(playerDistance);
                break;

            default:
                RunMovePlan(playerDistance);
                break;
        }
    }

    private void RunTeleportPlan(float playerDistance)
    {
    }

    private void RunJumpPlan(float playerDistance)
    {
    }

    private void RunEvadePlan(float playerDistance)
    {
    }

    private void RunMovePlan(float playerDistance)
    {
        if (selectedAttackEntry == null) return;

        if (UpdateBattleSpacingOnly(playerDistance))
        {
            CommitSelectedAttack();
        }
    }

    private void UseAbilityEntry(AbilityEntry selectedEntry)
    {
        if (selectedEntry == null) return;

        switch (selectedEntry.action)
        {
            case BattleAction.Attack:
                {
                    var mapped = selectedEntry.state ?? enemy.GetMappedAbilityState(selectedEntry.name);
                    var attackState = mapped as AttackStateBase<TEnemy>;
                    if (attackState == null) break;

                    var detail = FindAttackDetail(enemy.attackDetails, selectedEntry.name);
                    if (detail == null) break;

                    enemy.CurrentAbilityEntry = selectedEntry;
                    OnUseAttack(selectedEntry, attackState, detail);

                    break;
                }

            case BattleAction.Evade:
                {
                    var mapped = selectedEntry.state ?? enemy.GetMappedAbilityState(selectedEntry.name);
                    var evasionState = mapped as EvasionStateBase<TEnemy>;
                    if (evasionState == null) break;

                    enemy.CurrentAbilityEntry = selectedEntry;
                    OnUseEvade(selectedEntry, evasionState);
                    break;
                }

            case BattleAction.Teleport:
                {
                    var mapped = selectedEntry.state ?? enemy.GetMappedAbilityState(selectedEntry.name);
                    if (mapped as TeleportStateBase<TEnemy> == null) break;

                    enemy.CurrentAbilityEntry = selectedEntry;
                    OnUseTeleport(selectedEntry);
                    break;
                }

            case BattleAction.Custom:
                {
                    enemy.CurrentAbilityEntry = selectedEntry;
                    OnUseCustom(selectedEntry);
                    break;
                }

            case BattleAction.Jump:
                {
                    var mapped = selectedEntry.state ?? enemy.GetMappedAbilityState(selectedEntry.name);
                    var jumpState = mapped as JumpStateBase<TEnemy>;
                    if (jumpState == null) break;

                    enemy.CurrentAbilityEntry = selectedEntry;
                    OnUseJump(selectedEntry);
                    break;
                }

            default:
                break;

        }
    }

    protected virtual void OnUseAttack(AbilityEntry entry, AttackStateBase<TEnemy> attackState, AttackDetail detail)
    {
        attackState.Configure(detail, () => this);
        stateMachine.ChangeState(attackState);
    }

    protected virtual void OnUseEvade(AbilityEntry entry, EvasionStateBase<TEnemy> evasionState)
    {
        int evasionMode = UnityEngine.Random.Range(0, 2);

        if (evasionMode == 0)
        {
            AbilityEntry chosenAttackEntry = SelectRandomAttack();

            if (chosenAttackEntry != null)
            {
                EnemyState attackState = GetAttackState(chosenAttackEntry, () => this);
                if (attackState != null)
                {
                    float? min = chosenAttackEntry.rangeMin;
                    float? max = chosenAttackEntry.rangeMax;

                    evasionState.ConfigureMoveAndAttack(
                        next: () => this,
                        attack: () => attackState,
                        min: min,
                        max: max,
                        evasionDuration: entry.evasionDuration,
                        evasionSpeedMultiplier: entry.evasionSpeedMultiplier,
                        evadeBackAwayMin: entry.evadeBackAwayMin,
                        evadeBackAwayMax: entry.evadeBackAwayMax,
                        evadePassPastMin: entry.evadePassPastMin,
                        evadePassPastMax: entry.evadePassPastMax,
                        evadeReducedFactor: entry.evadeReducedFactor,
                        evadeTinyRetreat: entry.evadeTinyRetreat
                    );

                    stateMachine.ChangeState(evasionState);
                    return;
                }
            }
        }

        evasionState.ConfigureFlee(
            next: () => this,
            evasionDuration: entry.evasionDuration,
            evasionSpeedMultiplier: entry.evasionSpeedMultiplier,
            evadeBackAwayMin: entry.evadeBackAwayMin,
            evadeBackAwayMax: entry.evadeBackAwayMax,
            evadePassPastMin: entry.evadePassPastMin,
            evadePassPastMax: entry.evadePassPastMax,
            evadeReducedFactor: entry.evadeReducedFactor,
            evadeTinyRetreat: entry.evadeTinyRetreat
        );

        stateMachine.ChangeState(evasionState);
    }

    protected virtual void OnUseCustom(AbilityEntry entry) { }
    protected virtual void OnUseTeleport(AbilityEntry teleportEntry)
    {
        int mode = UnityEngine.Random.Range(0, 3);
        var selectAttack = SelectRandomAttack();

        var mapped = teleportEntry.state ?? enemy.GetMappedAbilityState(teleportEntry.name);
        var teleportState = mapped as TeleportStateBase<TEnemy>;
        if (teleportState == null) return;

        bool canChainTeleportAttack =
            selectAttack != null &&
            !selectAttack.cannotUseWithTeleport &&
            selectAttack.canAttackAfterTeleport;

        switch (mode)
        {
            // 0) Attack -> Teleport -> Battle
            case 0:
                {
                    if (canChainTeleportAttack)
                    {
                        enemy.CurrentAbilityEntry = selectAttack;

                        teleportState.Configure(() => this);

                        var attackState = GetAttackState(selectAttack, () => teleportState);
                        if (attackState != null)
                        {
                            stateMachine.ChangeState(attackState);
                            break;
                        }
                    }

                    teleportState.Configure(() => this);
                    stateMachine.ChangeState(teleportState);
                    break;
                }

            // 1) Teleport -> Attack -> Battle
            case 1:
                {
                    if (canChainTeleportAttack)
                    {
                        enemy.CurrentAbilityEntry = selectAttack;

                        var attackState = GetAttackState(selectAttack, () => this);
                        if (attackState != null)
                        {
                            teleportState.Configure(() => attackState);
                            stateMachine.ChangeState(teleportState);
                            break;
                        }
                    }

                    teleportState.Configure(() => this);
                    stateMachine.ChangeState(teleportState);
                    break;
                }

            default:
                {
                    teleportState.Configure(() => this);
                    stateMachine.ChangeState(teleportState);
                    break;
                }
        }
    }

    protected virtual void OnUseJump(AbilityEntry entry)
    {
        if (entry == null) return;

        var mapped = entry.state ?? enemy.GetMappedAbilityState(entry.name);
        var jumpState = mapped as JumpStateBase<TEnemy>;
        if (jumpState == null) return;

        jumpState.Configure(
            next: () => this,
            jumpVelocity: entry.jumpAbilityVelocity,
            isJumpBack: entry.jumpBack
        );

        stateMachine.ChangeState(jumpState);
    }

    #region State
    private EnemyState GetAttackState(AbilityEntry entry, System.Func<EnemyState> nextProvider)
    {
        if (entry == null) return null;

        var mappedState = entry.state ?? enemy.GetMappedAbilityState(entry.name);

        var attackState = mappedState as AttackStateBase<TEnemy>;
        if (attackState != null)
        {
            var detail = FindAttackDetail(enemy.attackDetails, entry.name);
            if (detail == null) return null;

            attackState.Configure(detail, nextProvider);
            return attackState;
        }

        return BuildCustomState(entry);
    }

    protected virtual EnemyState BuildCustomState(AbilityEntry entry)
    {
        return entry?.state ?? enemy.GetMappedAbilityState(entry.name);
    }

    #endregion
    #endregion


    #region Movement

    private bool UpdateBattleSpacingOnly(float playerDistance)
    {
        if (playerDistance >= preferredRangeMin && playerDistance <= preferredRangeMax)
        {
            StopBattleMovement();
            return true;
        }

        if (playerDistance < preferredRangeMin)
        {
            MoveAwayToPreferredRange();
            return false;
        }

        if (playerDistance > preferredRangeMax)
        {
            MoveTowardPreferredRange();
            return false;
        }

        return false;
    }
    private void MoveTowardPreferredRange()
    {
        if (player == null) return;
        if (enemy.IsWallDetected() || !enemy.IsGroundDetected()) return;

        FlipTowardsPlayer();
        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, enemy.rb.linearVelocity.y);
    }

    private void MoveAwayToPreferredRange()
    {
        if (player == null) return;
        if (enemy.IsWallDetected() || !enemy.IsGroundDetected()) return;

        float dx = player.position.x - enemy.transform.position.x;

        if (dx > 0f)
        {
            if (enemy.facingDir != -1)
                enemy.Flip();
        }
        else if (dx < 0f)
        {
            if (enemy.facingDir != 1)
                enemy.Flip();
        }

        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, enemy.rb.linearVelocity.y);
    }

    private void StopBattleMovement()
    {
        FlipTowardsPlayer();
        enemy.SetVelocity(0f, enemy.rb.linearVelocity.y);
    }


    #endregion

    #region Helpers
    private void CommitSelectedAttack()
    {
        if (selectedAttackEntry == null) return;

        UseAbilityEntry(selectedAttackEntry);

        cooldownSystem?.SetBattleCooldown();
        cooldownSystem?.SetAbilityCooldown(selectedAttackEntry.action, selectedAttackEntry.name);
    }

    private AttackDetail FindAttackDetail(System.Collections.Generic.List<AttackDetail> list, string name)
    {
        if (list == null || string.IsNullOrEmpty(name)) return null;
        foreach (var ad in list)
        {
            if (ad != null && ad.name == name)
                return ad;
        }
        return null;
    }

    private AbilityEntry SelectRandomAttack()
    {
        var map = enemy.abilityMap;
        if (map == null || map.Count == 0) return null;

        var candidates = new System.Collections.Generic.List<AbilityEntry>();
        foreach (var pair in map)
        {
            AbilityEntry entry = pair.Value;
            if (entry == null) continue;
            if (!entry.unlocked) continue;
            if (entry.action != BattleAction.Attack) continue;

            var state = entry.state ?? enemy.GetMappedAbilityState(entry.name);
            if (state == null) continue;

            candidates.Add(entry);
        }

        if (candidates.Count == 0) return null;

        return WeightedPickByChance(candidates);
    }

    private AbilityEntry WeightedPickByChance(System.Collections.Generic.List<AbilityEntry> candidates)
    {
        float totalWeight = 0f;
        foreach (var candidate in candidates)
            totalWeight += 1f + Mathf.Max(0f, candidate.chance);

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float accumulated = 0f;

        foreach (var candidate in candidates)
        {
            accumulated += 1f + Mathf.Max(0f, candidate.chance);
            if (roll < accumulated) return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    private void BuildAbilityLists(IDictionary<string, AbilityEntry> abilityMap)
    {
        attackAbilities.Clear();
        evadeAbilities.Clear();
        jumpAbilities.Clear();
        teleportAbilities.Clear();

        if (abilityMap == null || abilityMap.Count == 0)
            return;

        foreach (var kvp in abilityMap)
        {
            AbilityEntry entry = kvp.Value;
            if (entry == null)
                continue;

            switch (entry.action)
            {
                case BattleAction.Attack:
                    attackAbilities.Add(entry);
                    break;

                case BattleAction.Evade:
                    evadeAbilities.Add(entry);
                    break;

                case BattleAction.Jump:
                    jumpAbilities.Add(entry);
                    break;

                case BattleAction.Teleport:
                    teleportAbilities.Add(entry);
                    break;
            }
        }
    }

    private float GetPlayerDistance()
    {
        if (player == null) return 0f;

        return Vector2.Distance(player.position, enemy.transform.position);
    }

    private AbilityPreference GetAttackPreferenceFromDistance(float playerDistance)
    {
        if (playerDistance >= mixedAttackMinRange && playerDistance <= mixedAttackMaxRange)
            return AbilityPreference.Mixed;

        if (playerDistance <= shortAttackMaxRange)
            return AbilityPreference.Short;

        return AbilityPreference.Long;
    }
    #endregion

}
