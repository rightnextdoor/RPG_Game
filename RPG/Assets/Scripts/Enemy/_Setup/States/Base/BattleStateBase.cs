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

    private enum BattlePlanHandoff
    {
        Teleport,
        Jump,
        Evade,
        None
    }

    private AbilityPreference preferredAttackPreference = AbilityPreference.Mixed;
    private BattleAction currentPlanAction = BattleAction.None;
    private BattlePlanHandoff currentPlanHandoff = BattlePlanHandoff.Teleport;

    protected enum SupportPlanDecision
    {
        AttackThenAbility,
        AbilityThenAttack,
        AbilityOnly,
        SkipAbility
    }

    private readonly List<SupportPlanDecision> supportDecisionList = new();

    private float attackThenAbilityWeight = 3f;
    private float abilityThenAttackWeight = 2f;
    private float abilityOnlyWeight = 1.5f;
    private float skipAbilityWeight = 1f;

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

    private float preferredRangeMax = 0f;

    private bool hasTeleport = false;
    private bool hasJump = false;
    private bool hasEvade = false;

    private float jumpMaxRange = 0f;
    private float evadeMaxRange = 0f;

    private AbilityEntry selectedAttackEntry;
    private AbilityEntry selectedSupportEntry;
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

                case AbilityPreference.Mixed:
                    if (rangeMin > mixedAttackMinRange)
                        mixedAttackMinRange = rangeMin;

                    if (rangeMax > mixedAttackMaxRange)
                        mixedAttackMaxRange = rangeMax;
                    break;
            }
        }

        SetupMixedAttackFallback();
    }

    private void SetupMixedAttackFallback()
    {
        if (shortAttackMinRange <= 0f && shortAttackMaxRange > 0f)
            shortAttackMinRange = shortAttackMaxRange * 0.5f;

        if (longAttackMinRange <= 0f && longAttackMaxRange > 0f)
            longAttackMinRange = longAttackMaxRange * 0.5f;

        if (mixedAttackMinRange <= 0f && mixedAttackMaxRange > 0f)
            mixedAttackMinRange = mixedAttackMaxRange * 0.5f;

        if (mixedAttackMaxRange <= 0f)
        {
            if (longAttackMinRange > 0f)
                mixedAttackMaxRange = longAttackMinRange;
            else if (longAttackMaxRange > 0f)
                mixedAttackMaxRange = longAttackMaxRange * 0.5f;
            else if (shortAttackMaxRange > 0f)
                mixedAttackMaxRange = shortAttackMaxRange + (shortAttackMaxRange * 0.5f);
        }

        if (mixedAttackMinRange <= 0f)
        {
            if (shortAttackMaxRange > 0f)
                mixedAttackMinRange = shortAttackMaxRange;
            else if (longAttackMinRange > 0f)
                mixedAttackMinRange = longAttackMinRange * 0.5f;
            else if (longAttackMaxRange > 0f)
                mixedAttackMinRange = longAttackMaxRange * 0.25f;
        }
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
        preferredRangeMax = 0f;

        switch (preferredAttackPreference)
        {
            case AbilityPreference.Short:
                preferredRangeMax = shortAttackMaxRange;
                break;

            case AbilityPreference.Long:
                preferredRangeMax = longAttackMaxRange;
                break;

            case AbilityPreference.Mixed:
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

        HandoffToNextPlan();

        float updatedPlayerDistance = GetPlayerDistance();
        RunBattlePlanSwitch(updatedPlayerDistance);
    }
    #endregion

    #region Battle decision

    #region Plan handoff
    private void HandoffToNextPlan()
    {
        switch (currentPlanHandoff)
        {
            case BattlePlanHandoff.Teleport:
                if (hasTeleport)
                {
                    currentPlanAction = BattleAction.Teleport;
                    return;
                }

                currentPlanHandoff = BattlePlanHandoff.Jump;
                HandoffToNextPlan();
                return;

            case BattlePlanHandoff.Jump:
                if (hasJump)
                {
                    currentPlanAction = BattleAction.Jump;
                    return;
                }

                currentPlanHandoff = BattlePlanHandoff.Evade;
                HandoffToNextPlan();
                return;

            case BattlePlanHandoff.Evade:
                if (hasEvade)
                {
                    currentPlanAction = BattleAction.Evade;
                    return;
                }

                currentPlanHandoff = BattlePlanHandoff.None;
                HandoffToNextPlan();
                return;

            default:
                currentPlanAction = BattleAction.None;
                return;
        }
    }

    private void ResetHandoff()
    {
        currentPlanHandoff = BattlePlanHandoff.Teleport;
    }
    #endregion

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
        bool check = CheckSupportPlan(BattleAction.Teleport, AbilityPreference.Mixed, playerDistance, 0);
        if (!check)
        {
            currentPlanHandoff = BattlePlanHandoff.Jump;
            HandoffToNextPlan();
            return;
        }

        BuildSupportDecisionList(playerDistance);
        SupportPlanDecision decision = ChooseSupportDecision();
        DebugSupportDecision(decision, "Teleport");

        if (decision == SupportPlanDecision.SkipAbility)
        {
            currentPlanHandoff = BattlePlanHandoff.Jump;
            HandoffToNextPlan();
            return;
        }

        CommitSelectedAttack(selectedSupportEntry, decision);
    }

    private void RunJumpPlan(float playerDistance)
    {
        bool check = CheckSupportPlan(BattleAction.Jump, AbilityPreference.Short, playerDistance, jumpMaxRange);
        if (!check)
        {
            currentPlanHandoff = BattlePlanHandoff.Evade;
            HandoffToNextPlan();
            return;
        }

        BuildSupportDecisionList(playerDistance);
        SupportPlanDecision decision = ChooseSupportDecision();

        if (decision == SupportPlanDecision.SkipAbility)
        {
            currentPlanHandoff = BattlePlanHandoff.Evade;
            HandoffToNextPlan();
            return;
        }

        CommitSelectedAttack(selectedSupportEntry, decision);
    }

    private void RunEvadePlan(float playerDistance)
    {
        bool check = CheckSupportPlan(BattleAction.Evade, AbilityPreference.Short, playerDistance, evadeMaxRange);
        if (!check)
        {
            currentPlanHandoff = BattlePlanHandoff.None;
            HandoffToNextPlan();
            return;
        }

        BuildSupportDecisionList(playerDistance);
        SupportPlanDecision decision = ChooseSupportDecision();

        if (decision == SupportPlanDecision.SkipAbility)
        {
            currentPlanHandoff = BattlePlanHandoff.None;
            HandoffToNextPlan();
            return;
        }

        CommitSelectedAttack(selectedSupportEntry, decision);
    }

    private void RunMovePlan(float playerDistance)
    {
        if (!UpdateBattleSpacingOnly(playerDistance))
            return;

        RunSelectedAttackPlan(playerDistance);
    }

    #region Support check
    private bool CheckSupportPlan(BattleAction supportAction, AbilityPreference preference, float playerDistance, float maxRange)
    {
        selectedSupportEntry = null;

        if (!CheckSupportAbility(supportAction, preference))
        {
            HandoffToNextPlan();
            return false;
        }

        if (!CheckSupportRange(playerDistance, maxRange))
        {
            selectedSupportEntry = null;
            HandoffToNextPlan();
            return false;
        }

        return true;
    }

    private bool CheckSupportAbility(BattleAction supportAction, AbilityPreference preference)
    {
        if (hub == null)
            return false;

        selectedSupportEntry = hub.GetAbility(supportAction, preference);

        return selectedSupportEntry != null;
    }

    private bool CheckSupportRange(float playerDistance, float maxRange)
    {
        if (maxRange <= 0f)
            return true;

        return playerDistance <= maxRange;
    }

    private void RunSelectedAttackPlan(float playerDistance)
    {
        if (!CheckSelectedAttackRange(playerDistance))
        {
            HandoffToNextPlan();
            return;
        }

        CommitSelectedAttack(selectedAttackEntry);
    }

    private bool CheckSelectedAttackRange(float playerDistance)
    {
        if (selectedAttackEntry == null)
            return false;

        float minRange = selectedAttackEntry.rangeMin ?? 0f;
        float maxRange = selectedAttackEntry.rangeMax ?? 0f;

        if (playerDistance < minRange)
            return false;

        if (maxRange > 0f && playerDistance > maxRange)
            return false;

        return true;
    }


    #endregion

    #region Support decision
    private void BuildSupportDecisionList(float playerDistance)
    {
        supportDecisionList.Clear();

        if (CheckSelectedAttackRange(playerDistance))
            supportDecisionList.Add(SupportPlanDecision.AttackThenAbility);

        supportDecisionList.Add(SupportPlanDecision.AbilityThenAttack);
        supportDecisionList.Add(SupportPlanDecision.AbilityOnly);
        supportDecisionList.Add(SupportPlanDecision.SkipAbility);
    }

    private SupportPlanDecision ChooseSupportDecision()
    {
        float totalWeight = 0f;

        for (int i = 0; i < supportDecisionList.Count; i++)
        {
            switch (supportDecisionList[i])
            {
                case SupportPlanDecision.AttackThenAbility:
                    totalWeight += attackThenAbilityWeight;
                    break;

                case SupportPlanDecision.AbilityThenAttack:
                    totalWeight += abilityThenAttackWeight;
                    break;

                case SupportPlanDecision.AbilityOnly:
                    totalWeight += abilityOnlyWeight;
                    break;

                case SupportPlanDecision.SkipAbility:
                    totalWeight += skipAbilityWeight;
                    break;
            }
        }

        float roll = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < supportDecisionList.Count; i++)
        {
            switch (supportDecisionList[i])
            {
                case SupportPlanDecision.AttackThenAbility:
                    currentWeight += attackThenAbilityWeight;
                    break;

                case SupportPlanDecision.AbilityThenAttack:
                    currentWeight += abilityThenAttackWeight;
                    break;

                case SupportPlanDecision.AbilityOnly:
                    currentWeight += abilityOnlyWeight;
                    break;

                case SupportPlanDecision.SkipAbility:
                    currentWeight += skipAbilityWeight;
                    break;
            }

            if (roll <= currentWeight)
                return supportDecisionList[i];
        }

        return supportDecisionList[supportDecisionList.Count - 1];
    }

    private void DebugSupportDecision(SupportPlanDecision decision, string planName)
    {
        Debug.Log(planName + " picked decision: " + decision);
    }
    #endregion


    #endregion

    #region Setup next state
    private void UseAbilityEntry(AbilityEntry selectedEntry, SupportPlanDecision? decision = null)
    {
        if (selectedEntry == null)
            return;

        enemy.CurrentAbilityEntry = selectedEntry;

        switch (selectedEntry.action)
        {
            case BattleAction.Attack:
                OnUseAttack();
                break;

            case BattleAction.Evade:
                if (!decision.HasValue)
                    return;

                OnUseEvade(decision.Value);
                break;

            case BattleAction.Teleport:
                if (!decision.HasValue)
                    return;

                OnUseTeleport(decision.Value);
                break;

            case BattleAction.Jump:
                if (!decision.HasValue)
                    return;

                OnUseJump(decision.Value);
                break;
        }
    }

    protected virtual void OnUseAttack()
    {
        EnemyState attackState = GetAttackState();
        if (attackState == null)
            return;

        stateMachine.ChangeState(attackState);
    }

    protected virtual void OnUseEvade(SupportPlanDecision decision)
    {
        if (selectedSupportEntry == null)
            return;

        var mapped = selectedSupportEntry.state ?? enemy.GetMappedAbilityState(selectedSupportEntry.name);
        var evasionState = mapped as EvasionStateBase<TEnemy>;
        if (evasionState == null)
            return;

        EnemyState attackState = null;

        if (decision == SupportPlanDecision.AbilityThenAttack)
        {
            attackState = GetAttackState();
            if (attackState == null)
                return;

            evasionState.ConfigureMoveAndAttack(
                next: () => this,
                attack: () => attackState,
                entry: selectedSupportEntry,
                attackEntry: selectedAttackEntry
            );
        }
        else
        {
            if (decision == SupportPlanDecision.AttackThenAbility)
            {
                attackState = GetAttackState();
                if (attackState == null)
                    return;
            }

            evasionState.ConfigureFlee(
                next: () => this,
                entry: selectedSupportEntry
            );
        }

        EnemyState firstState = BuildNextStateFromDecision(
            decision,
            evasionState,
            attackState);

        if (firstState == null)
            return;

        stateMachine.ChangeState(firstState);
    }

    protected virtual void OnUseTeleport(SupportPlanDecision decision)
    {
        if (selectedSupportEntry == null)
            return;

        var mapped = selectedSupportEntry.state ?? enemy.GetMappedAbilityState(selectedSupportEntry.name);
        var teleportState = mapped as TeleportStateBase<TEnemy>;
        if (teleportState == null)
            return;

        teleportState.Configure(() => this);

        EnemyState attackState = null;

        if (decision != SupportPlanDecision.AbilityOnly)
        {
            attackState = GetAttackState();
            if (attackState == null)
                return;
        }

        EnemyState firstState = BuildNextStateFromDecision(
            decision,
            teleportState,
            attackState);

        if (firstState == null)
            return;

        stateMachine.ChangeState(firstState);
    }

    protected virtual void OnUseJump(SupportPlanDecision decision)
    {
        if (selectedSupportEntry == null)
            return;

        var mapped = selectedSupportEntry.state ?? enemy.GetMappedAbilityState(selectedSupportEntry.name);
        var jumpState = mapped as JumpStateBase<TEnemy>;
        if (jumpState == null)
            return;

        jumpState.Configure(
            next: () => this,
            jumpVelocity: selectedSupportEntry.jumpAbilityVelocity,
            isJumpBack: selectedSupportEntry.jumpBack
        );

        EnemyState attackState = null;

        if (decision != SupportPlanDecision.AbilityOnly)
        {
            attackState = GetAttackState();
            if (attackState == null)
                return;
        }

        EnemyState firstState = BuildNextStateFromDecision(
            decision,
            jumpState,
            attackState);

        if (firstState == null)
            return;

        stateMachine.ChangeState(firstState);
    }

    private EnemyState BuildNextStateFromDecision(
    SupportPlanDecision decision,
    EnemyState supportState,
    EnemyState attackState = null)
    {
        if (supportState == null)
            return null;

        switch (decision)
        {
            case SupportPlanDecision.AttackThenAbility:
                {
                    if (attackState == null)
                        return null;

                    var attack = attackState as AttackStateBase<TEnemy>;
                    if (attack == null)
                        return null;

                    attack.SetNextState(() => supportState);

                    if (supportState is TeleportStateBase<TEnemy> teleport)
                        teleport.SetNextState(() => this);
                    else if (supportState is JumpStateBase<TEnemy> jump)
                        jump.SetNextState(() => this);
                    else if (supportState is EvasionStateBase<TEnemy> evade)
                        evade.SetNextState(() => this);

                    return attackState;
                }

            case SupportPlanDecision.AbilityThenAttack:
                {
                    if (attackState == null)
                        return null;

                    var attack = attackState as AttackStateBase<TEnemy>;
                    if (attack == null)
                        return null;

                    attack.SetNextState(() => this);

                    if (supportState is TeleportStateBase<TEnemy> teleport)
                        teleport.SetNextState(() => attackState);
                    else if (supportState is JumpStateBase<TEnemy> jump)
                        jump.SetNextState(() => attackState);
                    else if (supportState is EvasionStateBase<TEnemy> evade)
                        evade.SetNextState(() => this);

                    return supportState;
                }

            case SupportPlanDecision.AbilityOnly:
                {
                    if (supportState is TeleportStateBase<TEnemy> teleport)
                        teleport.SetNextState(() => this);
                    else if (supportState is JumpStateBase<TEnemy> jump)
                        jump.SetNextState(() => this);
                    else if (supportState is EvasionStateBase<TEnemy> evade)
                        evade.SetNextState(() => this);

                    return supportState;
                }

            default:
                return null;
        }
    }

    #region State Helper
    private EnemyState GetAttackState()
    {
        if (selectedAttackEntry == null)
            return null;

        var mappedState = selectedAttackEntry.state ?? enemy.GetMappedAbilityState(selectedAttackEntry.name);
        var attackState = mappedState as AttackStateBase<TEnemy>;
        if (attackState == null)
            return null;

        var detail = FindAttackDetail(enemy.attackDetails, selectedAttackEntry.name);
        if (detail == null)
            return null;

        attackState.Configure(detail, () => this);
        return attackState;
    }

    #endregion

    #endregion

    #region Movement

    private bool UpdateBattleSpacingOnly(float playerDistance)
    {
        if (playerDistance <= preferredRangeMax)
        {
            StopBattleMovement();
            return true;
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
        enemy.SetVelocity(enemy.moveSpeed * enemy.battleSpeedMultiplier * enemy.facingDir, enemy.rb.linearVelocity.y);
    }

    private void StopBattleMovement()
    {
        FlipTowardsPlayer();
        enemy.SetVelocity(0f, enemy.rb.linearVelocity.y);
    }


    #endregion

    #region Helpers
    private void CommitSelectedAttack(AbilityEntry entry, SupportPlanDecision? decision = null)
    {
        if (entry == null)
            return;

        UseAbilityEntry(entry, decision);
        ResetBattleUseState(decision);
    }

    private void ResetBattleUseState(SupportPlanDecision? decision = null)
    {
        ResetHandoff();
        cooldownSystem?.SetBattleCooldown();

        if (decision.HasValue)
        {
            if (decision.Value == SupportPlanDecision.AbilityOnly)
            {
                if (selectedSupportEntry != null)
                    cooldownSystem?.SetAbilityCooldown(selectedSupportEntry.action, selectedSupportEntry.name);
            }
            else
            {
                if (selectedAttackEntry != null)
                    cooldownSystem?.SetAbilityCooldown(selectedAttackEntry.action, selectedAttackEntry.name);

                if (selectedSupportEntry != null)
                    cooldownSystem?.SetAbilityCooldown(selectedSupportEntry.action, selectedSupportEntry.name);
            }

            selectedAttackEntry = null;
            selectedSupportEntry = null;
            return;
        }

        if (selectedAttackEntry != null)
            cooldownSystem?.SetAbilityCooldown(selectedAttackEntry.action, selectedAttackEntry.name);

        selectedAttackEntry = null;
        selectedSupportEntry = null;
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
