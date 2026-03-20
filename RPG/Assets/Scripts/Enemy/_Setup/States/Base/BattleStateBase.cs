using System.Collections.Generic;
using UnityEngine;

public class BattleStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    private readonly System.Func<EnemyState> idleState;
    private readonly System.Func<EnemyState> nextState;

    private readonly List<StateSound> enterSounds;
    private readonly List<StateSound> exitSounds;

    private Transform player;

    private readonly AbilityHub hub = new AbilityHub();
    private float nextHubTime;
    private const float hubIntervalMin = 0.15f;
    private const float hubIntervalMax = 0.30f;

    private float stopDistance = 0f;
    private bool hasStopDistance = false;

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


    public override void Enter()
    {
        base.Enter();

        enemy.CurrentAbilityEntry = null;

        player = PlayerUtils.GetPlayerSafe().transform;

        if (player.GetComponent<PlayerStats>().isDead && nextState != null)
        {
            stateMachine.ChangeState(nextState());
            return;
        }

        ComputeStopDistanceFromAttackDetails();

        nextHubTime = Time.time;

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

    private void PlayerDetected()
    {
        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;

            DecideFromHub();
        }
        else
        {
            if (stateTimer < 0f ||
                Vector2.Distance(player.position, enemy.transform.position) > enemy.playerDistance)
            {
                if (idleState != null)
                    stateMachine.ChangeState(idleState());
            }
        }

        FlipTowardsPlayer();
    }

    private void FlipTowardsPlayer()
    {
        if (player == null) return;

        float dx = player.position.x - enemy.transform.position.x;
        if (dx > 0f && enemy.facingDir == -1) enemy.Flip();
        else if (dx < 0f && enemy.facingDir == 1) enemy.Flip();
    }

    protected virtual void DecideFromHub()
    {
        float now = Time.time;
        ApplyMovementTowardStopDistance();

        if (now < nextHubTime) return;

        float distance = Vector2.Distance(player.position, enemy.transform.position);

        var abilityMap = enemy.abilityMap;
        //update for new hub
        //AbilityEntry entry = hub.TryPick(abilityMap, distance);
        AbilityEntry entry = null;

        if (entry != null)
            UseAbilityEntry(entry);

        nextHubTime = now + Random.Range(hubIntervalMin, hubIntervalMax);
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

    private void ApplyMovementTowardStopDistance()
    {
        if (player == null) return;

        float distance = Vector2.Distance(player.position, enemy.transform.position);

        bool shouldAdvance =
            !hasStopDistance ? true : distance > stopDistance;

        if (shouldAdvance)
        {
            if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
                return;

            enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, enemy.rb.linearVelocity.y);
        }

    }

    private void ComputeStopDistanceFromAttackDetails()
    {
        hasStopDistance = false;
        stopDistance = 0f;

        var details = enemy.attackDetails;
        if (details == null || details.Count == 0) return;

        float best = float.PositiveInfinity;

        for (int i = 0; i < details.Count; i++)
        {
            var detail = details[i];
            if (detail == null) continue;

            float threshold = 0f;

            if (detail.rangeMax > 0f)
                threshold = detail.rangeMax;
            else if (detail.rangeMin > 0f)
                threshold = detail.rangeMin;
            else
                continue;

            if (threshold < best)
                best = threshold;
        }

        if (best < float.PositiveInfinity)
        {
            stopDistance = best;
            hasStopDistance = true;
        }
    }
}
