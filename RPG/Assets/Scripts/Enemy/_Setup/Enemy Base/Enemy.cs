using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EntityFX))]
[RequireComponent(typeof(ItemDrop))]
public class Enemy : Entity
{
    [SerializeField] protected LayerMask whatIsPlayer;
    [Header("Player detected")]
    public float playerDistance = 10;
    public float idleTime = 2;

    [Header("Move info")]
    public bool canPatrol = true; // remove after update of all enemies
    public float moveSpeed = 1.5f;

    public float moveTime = 5;
    public float battleTime = 7;
    private float defaultMoveSpeed;

    #region Old settings
    [Header("Evasion info_Old")]
    public float evasionTimer = 1.5f;
    public float evasionSpeed = 10f;
    [SerializeField] private float evasionDistance = 1f; //How close player should be to trigger before evading
    [SerializeField] private float minEvasionCooldown = 0f;
    [SerializeField] private float maxEvasionCooldown = .5f;
    private float lastTimeEvade;

    [Header("Range Attack info old")]
    public float agroDistance = 2;
    public float rangeAttackDistance = 2;
    [HideInInspector] public float rangeAttackCooldown;
    public float minRangeAttackCooldown = 1;
    public float maxRangeAttackCooldown = 2;
    [HideInInspector] public float lastTimeRangeAttacked;

    [Header("Melee Attack info old")]
    public float meleeAttackDistance = 2;
    [HideInInspector] public float meleeAttackCooldown;
    public float minMeleeAttackCooldown = 1;
    public float maxMeleeAttackCooldown = 2;
    [HideInInspector] public float lastTimeMeleeAttacked;
    #endregion

    [Space(2)]
    [Header("State Details")]
    public List<StateDetail> stateDetails = new List<StateDetail>();

    [Space(2)]
    [Header("Attack Details")]
    public List<AttackDetail> attackDetails = new List<AttackDetail>();
    [HideInInspector] public Dictionary<string, AbilityEntry> abilityMap;
    [HideInInspector] public AbilityEntry CurrentAbilityEntry { get; set; }
    protected AttackDetail currentAttackDetail;

    [Space(2)]
    [Header("Battle Settings")]
    public AbilityPreference battlePreference = AbilityPreference.Mixed;
    public float battleMinCooldown = 1.5f;
    public float battleMaxCooldown = 2.5f;
    protected bool battleStarted;

    [HideInInspector] public bool isSummon = false;

    public EnemyStateMachine stateMachine { get; private set; }
    public EntityFX fX { get; private set; }
    public string lastAnimBoolName { get; private set; }

    #region Setup
    protected override void Awake()
    {
        base.Awake();

        stateMachine = new EnemyStateMachine();

        defaultMoveSpeed = moveSpeed;

        abilityMap = new Dictionary<string, AbilityEntry>();
    }

    protected override void Start()
    {
        base.Start();

        fX = GetComponent<EntityFX>();
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
    }
    public virtual void AssignLastAnimName(string _animBoolName) => lastAnimBoolName = _animBoolName;
    #endregion

    #region Ability Mapping

    protected virtual void MapAbilityStates()
    {
        MapAttackDetailsToAbilityEntries();
    }

    private void MapAttackDetailsToAbilityEntries()
    {
        if (abilityMap == null)
            abilityMap = new Dictionary<string, AbilityEntry>();
        else
            abilityMap.Clear();

        if (attackDetails == null || attackDetails.Count == 0)
            return;

        foreach (var attackDetail in attackDetails)
        {
            if (attackDetail == null || string.IsNullOrWhiteSpace(attackDetail.name))
                continue;

            var entry = new AbilityEntry(
                name: attackDetail.name,
                animBoolName: attackDetail.animBoolName,
                stateName: attackDetail.stateName,
                state: null,
                minCooldown: attackDetail.minCooldown,
                maxCooldown: attackDetail.maxCooldown,
                startCooldown: attackDetail.startCooldown,
                action: attackDetail.action,
                preference: attackDetail.preference,
                initialCooldown: 0f,
                rangeMin: attackDetail.rangeMin,
                rangeMax: attackDetail.rangeMax,
                unlocked: attackDetail.unlocked,
                chance: attackDetail.chance,
                cannotUseWithTeleport: attackDetail.cannotUseWithTeleport,
                canAttackAfterTeleport: attackDetail.canAttackAfterTeleport,

                lingerTime: attackDetail.lingerTime,
                attackAmount: attackDetail.attackAmount,
                attackTimeMin: attackDetail.attackTimeMin,
                attackTimeMax: attackDetail.attackTimeMax,

                evasionSpeedMultiplier: attackDetail.evasionSpeedMultiplier,
                evasionDuration: attackDetail.evasionDuration,
                evadeBackAwayMin: attackDetail.evadeBackAwayMin,
                evadeBackAwayMax: attackDetail.evadeBackAwayMax,
                evadePassPastMin: attackDetail.evadePassPastMin,
                evadePassPastMax: attackDetail.evadePassPastMax,
                evadeReducedFactor: attackDetail.evadeReducedFactor,
                evadeTinyRetreat: attackDetail.evadeTinyRetreat,

                stunDuration: attackDetail.stunDuration,
                stunDirection: attackDetail.stunDirection,
                canBeStunned: attackDetail.canBeStunned,
                counterImage: attackDetail.counterImage,

                jumpAbilityVelocity: attackDetail.jumpAbilityVelocity,
                jumpBack: attackDetail.jumpBack
            );

            abilityMap[attackDetail.name] = entry;
        }
    }

    public virtual EnemyState GetMappedAbilityState(string name)
    {
        if (abilityMap != null && name != null &&
            abilityMap.TryGetValue(name, out var entry) && entry != null)
        {
            return entry.state;
        }
        return null;
    }

    public virtual StateDetail GetStateDetail(EnemyStateType stateType)
    {
        if (stateDetails == null || stateDetails.Count == 0)
            return null;

        for (int i = 0; i < stateDetails.Count; i++)
        {
            var detail = stateDetails[i];
            if (detail != null && detail.stateType == stateType)
                return detail;
        }

        return null;
    }

    public virtual StateDetail GetStateDetailByName(string stateName)
    {
        if (stateDetails == null || stateDetails.Count == 0 || string.IsNullOrWhiteSpace(stateName))
            return null;

        for (int i = 0; i < stateDetails.Count; i++)
        {
            var detail = stateDetails[i];
            if (detail != null && detail.name == stateName)
                return detail;
        }

        return null;
    }

    #endregion

    #region Attack Triggers
    public virtual void AttackTrigger(string pointName)
    {
        if (currentAttackDetail == null || string.IsNullOrWhiteSpace(pointName))
            return;

        if (!pointName.StartsWith("attackPoint"))
            return;

        string numberText = pointName.Substring("attackPoint".Length);
        if (!int.TryParse(numberText, out int attackPoint))
            return;

        List<AttackSpawnSpec> selectedSpecs = null;
        if (currentAttackDetail.spawnSpec != null)
        {
            selectedSpecs = new List<AttackSpawnSpec>();

            foreach (var spec in currentAttackDetail.spawnSpec)
            {
                if (spec == null || spec.attackPoints == null)
                    continue;

                if (spec.attackPoints.Contains(attackPoint))
                    selectedSpecs.Add(spec);
            }
        }

        if (currentAttackDetail.attackChecks == null || currentAttackDetail.attackChecks.Count == 0)
            return;

        foreach (var check in currentAttackDetail.attackChecks)
        {
            if (check == null || check.attackPoints == null)
                continue;

            if (!check.attackPoints.Contains(attackPoint))
                continue;

            if (check.checkTransform == null)
                continue;

            if (check.shape == AttackCheckShape.Point)
            {
                if (selectedSpecs == null || selectedSpecs.Count == 0)
                    continue;

                SpecialAttack(selectedSpecs, check);
                continue;
            }

            ResolveCollision(check);
        }
    }

    private void SpecialAttack(List<AttackSpawnSpec> specs, AttackCheck attackCheck)
    {
        if (specs == null || specs.Count == 0 || attackCheck == null || attackCheck.checkTransform == null) return;

        foreach (var spec in specs)
        {
            if (spec == null) continue;

            if (spec.prefab != null)
            {
                GameObject spawnedObject = Instantiate(spec.prefab, attackCheck.checkTransform.position, Quaternion.identity);
                SpecialAttackControl prefabControl = spawnedObject.GetComponent<SpecialAttackControl>();

                if (prefabControl != null)
                    prefabControl.Setup(stats, specs);

                continue;
            }

            if (spec.control != null)
                spec.control.Setup(stats, specs);
        }
    }
    public virtual void SoundTrigger(string pointName)
    {
        if (currentAttackDetail == null || currentAttackDetail.sounds == null || string.IsNullOrWhiteSpace(pointName))
            return;

        if (!pointName.StartsWith("soundPoint"))
            return;

        string numberText = pointName.Substring("soundPoint".Length);
        if (!int.TryParse(numberText, out int soundPoint))
            return;

        foreach (var sound in currentAttackDetail.sounds)
        {
            if (sound.soundPoints == null)
                continue;

            if (!sound.soundPoints.Contains(soundPoint))
                continue;

            sound.Play(transform);
        }
    }

    public void SetCurrentAttackDetail(AttackDetail attackDetail)
    {
        currentAttackDetail = attackDetail;
    }

    public void ClearCurrentAttackDetail()
    {
        currentAttackDetail = null;
    }

    #region Trigger helpers
    public virtual void AnimationFinishTrigger() => stateMachine.currentState.AnimationFinishTrigger();
    private static bool PointInPolygon(List<Vector2> poly, Vector2 p)
    {
        bool inside = false;
        int n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[j];
            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + Mathf.Epsilon) + a.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }
    private void ResolveCollision(AttackCheck check)
    {
        Vector2 pos = check.checkTransform.position;
        float rotZ = check.checkTransform.eulerAngles.z;

        switch (check.shape)
        {
            case AttackCheckShape.Circle:
                {
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(pos, Mathf.Max(0f, check.circleRadius));
                    foreach (var hit in colliders)
                    {
                        if (hit != null && hit.GetComponent<Player>() != null)
                        {
                            PlayerStats target = hit.GetComponent<PlayerStats>();
                            stats.DoDamage(target);
                        }
                    }
                    break;
                }

            case AttackCheckShape.Box:
                {
                    Vector2 size = new Vector2(Mathf.Max(0f, check.boxSize.x), Mathf.Max(0f, check.boxSize.y));
                    Collider2D[] colliders = Physics2D.OverlapBoxAll(pos, size, rotZ);
                    foreach (var hit in colliders)
                    {
                        if (hit != null && hit.GetComponent<Player>() != null)
                        {
                            PlayerStats target = hit.GetComponent<PlayerStats>();
                            stats.DoDamage(target);
                        }
                    }
                    break;
                }

            case AttackCheckShape.Capsule:
                {
                    Vector2 size = new Vector2(Mathf.Max(0f, check.capsuleSize.x), Mathf.Max(0f, check.capsuleSize.y));
                    var dir = (check.capsuleDirection == AttackCheck.CapsuleDirection.Horizontal)
                              ? CapsuleDirection2D.Horizontal
                              : CapsuleDirection2D.Vertical;

                    Collider2D[] colliders = Physics2D.OverlapCapsuleAll(pos, size, dir, rotZ);
                    foreach (var hit in colliders)
                    {
                        if (hit != null && hit.GetComponent<Player>() != null)
                        {
                            PlayerStats target = hit.GetComponent<PlayerStats>();
                            stats.DoDamage(target);
                        }
                    }
                    break;
                }

            case AttackCheckShape.Line:
                {
                    float len = Mathf.Max(0f, check.lineLength);
                    float thick = Mathf.Max(0.01f, check.lineThickness);
                    Vector2 size = new Vector2(len, thick);

                    Collider2D[] colliders = Physics2D.OverlapBoxAll(pos, size, rotZ);
                    foreach (var hit in colliders)
                    {
                        if (hit != null && hit.GetComponent<Player>() != null)
                        {
                            PlayerStats target = hit.GetComponent<PlayerStats>();
                            stats.DoDamage(target);
                        }
                    }
                    break;
                }

            case AttackCheckShape.Arc:
                {
                    float rOuter = Mathf.Max(0f, check.arcRadius);
                    float rInner = Mathf.Max(0f, check.arcInnerRadius);
                    float halfAng = Mathf.Abs(check.arcAngleDegrees) * 0.5f;

                    Vector2 fwd = check.checkTransform.right;
                    if (fwd.sqrMagnitude > 0f) fwd.Normalize();

                    Collider2D[] colliders = Physics2D.OverlapCircleAll(check.checkTransform.position, rOuter, whatIsPlayer);

                    pos = check.checkTransform.position;
                    foreach (var hit in colliders)
                    {
                        if (hit == null) continue;
                        var player = hit.GetComponent<Player>();
                        if (player == null) continue;

                        Vector2 to = (Vector2)hit.bounds.center - pos;
                        float d = to.magnitude;
                        if (d <= rInner || d > rOuter) continue;

                        float ang = Vector2.Angle(fwd, to);
                        if (ang <= halfAng)
                        {
                            var target = hit.GetComponent<PlayerStats>();
                            if (target != null) stats.DoDamage(target);
                        }
                    }
                    break;
                }

            case AttackCheckShape.Polygon:
                {
                    if (check.polygonPoints == null || check.polygonPoints.Count < 3) break;

                    // Broad circle around polygon extent
                    float maxR = 0f;
                    for (int i = 0; i < check.polygonPoints.Count; i++)
                        maxR = Mathf.Max(maxR, check.polygonPoints[i].magnitude);

                    Collider2D[] colliders = Physics2D.OverlapCircleAll(pos, Mathf.Max(0.01f, maxR));

                    // Transform polygon to world once
                    var world = new List<Vector2>(check.polygonPoints.Count);
                    for (int i = 0; i < check.polygonPoints.Count; i++)
                        world.Add(check.checkTransform.TransformPoint(check.polygonPoints[i]));

                    foreach (var hit in colliders)
                    {
                        if (hit == null) continue;
                        Vector2 center = hit.bounds.center;
                        if (PointInPolygon(world, center) && hit.GetComponent<Player>() != null)
                        {
                            PlayerStats target = hit.GetComponent<PlayerStats>();
                            stats.DoDamage(target);
                        }
                    }
                    break;
                }

            default:
                break;
        }
    }
    public virtual void OpenCounterAttackWindow()
    {
    }
    public virtual void CloseCounterAttackWindow()
    {
    }

    #endregion

    #region Old Triggers need to be remove
    public virtual void AnimationSpecialAttackTrigger()
    {

    }
    #endregion
    #endregion


    #region Enemy Config

    public virtual void ChangeBattleCooldownRange(float minCooldown, float maxCooldown)
    {
    }

    public virtual void TempChangeBattleCooldownRange(float minCooldown, float maxCooldown, float duration)
    {
    }

    public virtual void PauseBattleCooldown(bool pause)
    {
    }

    public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {
        moveSpeed = moveSpeed * (1 - _slowPercentage);
        anim.speed = anim.speed * (1 - _slowPercentage);

        float slowMultiplier = 1f + _slowPercentage;

        float slowedBattleMin = battleMinCooldown * slowMultiplier;
        float slowedBattleMax = battleMaxCooldown * slowMultiplier;

        TempChangeBattleCooldownRange(slowedBattleMin, slowedBattleMax, _slowDuration);

        Invoke("ReturnDefaultSpeed", _slowDuration);
    }

    public virtual void FreezeTime(bool _timeFrozen)
    {
        if (_timeFrozen)
        {
            moveSpeed = 0;
            anim.speed = 0;
        }
        else
        {
            moveSpeed = defaultMoveSpeed;
            anim.speed = 1;
        }
    }
    public virtual void FreezeTimeFor(float _duration) => StartCoroutine(FreezeTimerCoroutine(_duration));
    protected virtual IEnumerator FreezeTimerCoroutine(float _seconds)
    {
        PauseBattleCooldown(true);
        FreezeTime(true);

        yield return new WaitForSeconds(_seconds);

        FreezeTime(false);
        PauseBattleCooldown(false);
    }
    protected override void ReturnDefaultSpeed()
    {
        base.ReturnDefaultSpeed();

        moveSpeed = defaultMoveSpeed;
    }
    public virtual RaycastHit2D IsPlayerDetected()
    {
        Transform player = PlayerManager.instance.player.transform;
        if (player.GetComponent<PlayerStats>().isDead)
            return default(RaycastHit2D);

        Vector3 direction = player.position - wallCheck.position;
        RaycastHit2D playerDetected = Physics2D.Raycast(wallCheck.position, direction, playerDistance, whatIsPlayer);
        RaycastHit2D wallDetected = Physics2D.Raycast(wallCheck.position, direction, playerDistance, whatIsGround);


        if (wallDetected)
        {
            if (wallDetected.distance < playerDetected.distance)
            {
                if (wallDetected.collider.gameObject.tag == "Platform")
                    return playerDetected;
                Debug.DrawRay(wallCheck.position, direction, Color.red);
                return default(RaycastHit2D);
            }
        }
        Debug.DrawRay(wallCheck.position, direction, Color.green);
        return playerDetected;
    }
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

#if UNITY_EDITOR
        // Draw per-attack check gizmos (unchanged: they already use local transform of each check)
        if (attackDetails != null)
        {
            foreach (var ad in attackDetails)
            {
                if (ad == null || ad.attackChecks == null) continue;
                foreach (var c in ad.attackChecks)
                    c?.DrawGizmos();
            }
        }

        // Helper: local right scaled by facingDir
        Vector3 fwd = transform.right;

        // Ability ranges (per attack)
        if (attackDetails != null && attackDetails.Count > 0)
        {
            const float laneStepY = 0.15f;
            const float baseYOffset = 0.25f;
            const float stubLength = 0.5f;
            const float endTick = 0.03f;

            for (int i = 0; i < attackDetails.Count; i++)
            {
                var ad = attackDetails[i];
                if (ad == null) continue;

                float minR = Mathf.Max(0f, ad.rangeMin);
                float maxR = Mathf.Max(minR, ad.rangeMax);
                if (maxR <= minR) maxR = minR + stubLength;

                Gizmos.color = Color.HSVToRGB((i * 0.13f) % 1f, 0.8f, 1f);

                float yLane = baseYOffset + i * laneStepY;
                Vector3 laneOrigin = transform.position + new Vector3(0f, yLane, 0f);

                Vector3 segStart = laneOrigin + fwd.normalized * minR;
                Vector3 segEnd = laneOrigin + fwd.normalized * maxR;
                Gizmos.DrawLine(segStart, segEnd);

                Vector3 up = Vector3.up * endTick; // purely visual tick
                Gizmos.DrawLine(segStart - up, segStart + up);
                Gizmos.DrawLine(segEnd - up, segEnd + up);
            }
        }

        // Player detection distance lane
        {
            const float laneStepY = 0.15f;
            const float baseYOffset = 0.25f;
            const float endTick = 0.03f;

            float dist = Mathf.Max(0f, playerDistance);
            float yLaneBelow = baseYOffset - (laneStepY * 3f);

            Vector3 origin = transform.position + new Vector3(0f, yLaneBelow, 0f);
            Vector3 end = origin + fwd.normalized * dist;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, end);

            Vector3 up = Vector3.up * endTick;
            Gizmos.DrawLine(end - up, end + up);
        }

        // Evasion trigger distance lane
        {
            const float laneStepY = 0.15f;
            const float baseYOffset = 0.25f;
            const float endTick = 0.03f;

            float dist = Mathf.Max(0f, evasionDistance);
            float yLaneBelow = (baseYOffset - (laneStepY * 5f));
            Vector3 origin = transform.position + new Vector3(0f, yLaneBelow, 0f);
            Vector3 end = origin + fwd.normalized * dist;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, end);

            Vector3 up = Vector3.up * endTick;
            Gizmos.DrawLine(end - up, end + up);
        }

        // Evade range band
        {
            const float laneStepY = 0.15f;
            const float baseYOffset = 0.25f;
            const float endTick = 0.03f;
            const float stubLength = 0.5f;

            float minR = -1f;
            float maxR = -1f;

            if (attackDetails != null)
            {
                foreach (var ad in attackDetails)
                {
                    if (ad == null || ad.action != BattleAction.Evade)
                        continue;

                    float detailMin = Mathf.Max(0f, ad.rangeMin);
                    float detailMax = Mathf.Max(detailMin, ad.rangeMax);

                    if (minR < 0f || detailMin < minR)
                        minR = detailMin;

                    if (maxR < 0f || detailMax > maxR)
                        maxR = detailMax;
                }
            }

            if (minR < 0f || maxR < 0f)
                return;

            if (maxR <= minR)
                maxR = minR + stubLength;

            float yLaneBelow = baseYOffset - (laneStepY * 7f);
            Vector3 laneOrigin = transform.position + new Vector3(0f, yLaneBelow, 0f);

            Vector3 segStart = laneOrigin + fwd.normalized * minR;
            Vector3 segEnd = laneOrigin + fwd.normalized * maxR;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(segStart, segEnd);

            Vector3 up = Vector3.up * endTick;
            Gizmos.DrawLine(segStart - up, segStart + up);
            Gizmos.DrawLine(segEnd - up, segEnd + up);
        }
#endif
    }

    public virtual void StartBattle()
    {
        if (battleStarted)
            return;

        battleStarted = true;
    }

    public void EndBattle()
    {
        battleStarted = false;
    }
    public bool BattleStarted => battleStarted;
    #endregion

    #region Combat Helper
    public virtual void SelfDestroy()
    {

    }
    
    public virtual bool CanBeStunned()
    {
        return false;
    }
    public bool IsSummon()
    {
        return isSummon;
    }
    #endregion


    #region Battle States old system need be remove when all enemy is updated

    public void RangeAttack(Transform player, EnemyState attackState, EnemyState evasionState, string audioName)
    {
        if (IsPlayerDetected().distance > rangeAttackDistance)
        {
            if (IsWallDetected() || !IsGroundDetected())
                return;

            SetVelocity(moveSpeed * facingDir, rb.linearVelocity.y);
        }

        if (IsPlayerDetected().distance <= rangeAttackDistance)
        {
            if (player != null)
            {
                if (Vector2.Distance(player.transform.position, transform.position) < evasionDistance)
                {
                    if (evasionState != null)
                    {
                        if (CanEvade())
                            stateMachine.ChangeState(evasionState);
                    }

                }
            }

            if (CanRangeAttack())
            {
                if (attackState != null)
                    stateMachine.ChangeState(attackState);

                if (audioName != null)
                    AudioManager.instance.PlaySFX(audioName, transform);
            }
        }
    }

    public void MultiMeleeAttack(Transform player, System.Collections.Generic.List<EnemyState> attackStates, EnemyState evasionState, string audioName, bool soundDelay, float delayTime)
    {
        if (IsPlayerDetected().distance > meleeAttackDistance)
        {
            if (IsWallDetected() || !IsGroundDetected())
                return;

            SetVelocity(moveSpeed * facingDir, rb.linearVelocity.y);

        }
        if (IsPlayerDetected().distance <= meleeAttackDistance)
        {
            if (player != null)
            {
                if (Vector2.Distance(player.transform.position, transform.position) < evasionDistance)
                {
                    if (evasionState != null)
                    {
                        if (CanEvade())
                            stateMachine.ChangeState(evasionState);
                    }
                }
            }

            if (CanMeleeAttack())
            {
                if (attackStates == null)
                    return;

                EnemyState attackState = null;

                int number = Random.Range(0, attackStates.Count);

                attackState = attackStates[number];

                if (attackState != null)
                    stateMachine.ChangeState(attackState);

                if (audioName != null)
                {
                    if (soundDelay)
                    {
                        AudioManager.instance.PlaySFXWithDelay(audioName, delayTime);
                    }
                    else
                        AudioManager.instance.PlaySFX(audioName, transform);
                }
            }
        }
    }

    public void MeleeAttack(Transform player, EnemyState attackState, EnemyState evasionState, string audioName, bool soundDelay, float delayTime)
    {
        if (IsPlayerDetected().distance > meleeAttackDistance)
        {
            if (IsWallDetected() || !IsGroundDetected())
                return;

            SetVelocity(moveSpeed * facingDir, rb.linearVelocity.y);

        }
        if (IsPlayerDetected().distance <= meleeAttackDistance)
        {
            if (player != null)
            {
                if (Vector2.Distance(player.transform.position, transform.position) < evasionDistance)
                {
                    if (evasionState != null)
                    {
                        if (CanEvade())
                            stateMachine.ChangeState(evasionState);
                    }
                }
            }

            if (CanMeleeAttack())
            {
                if (attackState != null)
                    stateMachine.ChangeState(attackState);

                if (audioName != null)
                {
                    if (soundDelay)
                    {
                        AudioManager.instance.PlaySFXWithDelay(audioName, delayTime);
                    }
                    else
                        AudioManager.instance.PlaySFX(audioName, transform);
                }
            }
        }
    }

    public bool CanRangeAttack()
    {
        if (Time.time >= lastTimeRangeAttacked + rangeAttackCooldown)
        {
            rangeAttackCooldown = Random.Range(minRangeAttackCooldown, maxRangeAttackCooldown);
            lastTimeRangeAttacked = Time.time;
            return true;
        }
        return false;
    }

    private bool CanMeleeAttack()
    {
        if (Time.time >= lastTimeMeleeAttacked + meleeAttackCooldown)
        {
            meleeAttackCooldown = Random.Range(minMeleeAttackCooldown, maxMeleeAttackCooldown);
            lastTimeMeleeAttacked = Time.time;
            return true;
        }
        return false;
    }

    private bool CanEvade()
    {
        return false;
        //if (Time.time >= lastTimeEvade + evasionCooldown)
        //{
        //    evasionCooldown = Random.Range(minEvasionCooldown, maxEvasionCooldown);
        //    lastTimeEvade = Time.time;
        //    return true;
        //}
        //return false;
    }

    public void RunIntoPlayerAttack(EnemyState moveState)
    {
        //Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, attackCheckRadius);
        //bool hitPlayer = false;
        //foreach (var hit in colliders)
        //{
        //    if (hit.GetComponent<Player>() != null)
        //    {
        //        PlayerStats target = hit.GetComponent<PlayerStats>();
        //        if (!hitPlayer)
        //        {
        //            stats.DoDamage(target);
        //            hitPlayer = true;
        //            if(moveState != null)
        //                stateMachine.ChangeState(moveState);
        //        }
        //    }
        //}
    }

    public void BattleStateFlipControll(Transform player)
    {
        if (player.position.x > transform.position.x && facingDir == -1)
            Flip();
        else if (player.position.x < transform.position.x && facingDir == 1)
            Flip();
    }

    #endregion
}
