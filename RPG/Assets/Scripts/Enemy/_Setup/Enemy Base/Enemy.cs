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
    public bool canPatrol = true;
    public float moveSpeed = 1.5f;

    public float moveTime = 5;
    public float battleTime = 7;
    private float defaultMoveSpeed;

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

    [Header("Attack Details")]
    public List<AttackDetail> attackDetails = new List<AttackDetail>();
    [HideInInspector] public Dictionary<string, AbilityEntry> abilityMap;
    [HideInInspector] public bool teleportForAttack = false;
    [HideInInspector] public AbilityEntry CurrentAbilityEntry { get; set; }


    #region Evasion
    [Header("Evasion")]
    [Space(2)]
    [SerializeField] public float evadeRangeMin = 0f;
    [SerializeField] public float evadeRangeMax = 1f;
    [SerializeField] public float evasionSpeedMultiplier = 1.25f;
    [SerializeField] public float evasionDuration = 0.5f;
    [SerializeField] public float evasionCooldownMin = 1f;
    [SerializeField] public float evasionCooldownMax = 3f;

    [SerializeField] public float evadeBackAwayMin = 1.2f;
    [SerializeField] public float evadeBackAwayMax = 2.0f;
    [SerializeField] public float evadePassPastMin = 0.6f;
    [SerializeField] public float evadePassPastMax = 1.0f;
    [SerializeField] public float evadeReducedFactor = 0.6f;  // distance reduction for second-round attempts
    [SerializeField] public float evadeTinyRetreat = 0.35f; // small step-back when already inside band (M&A)

    #endregion

    [Header("Evasion info_Old")]
    public float evasionTimer = 1.5f;
    public float evasionSpeed = 10f;
    [SerializeField] private float evasionDistance = 1f; //How close player should be to trigger before evading
    [SerializeField] private float minEvasionCooldown = 0f;
    [SerializeField] private float maxEvasionCooldown = .5f;
    private float lastTimeEvade;

    [HideInInspector] public bool isSummon = false;

    public EnemyStateMachine stateMachine { get; private set; }
    public EntityFX fX { get; private set; }
    public string lastAnimBoolName { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        stateMachine = new EnemyStateMachine();

        defaultMoveSpeed = moveSpeed;

        abilityMap = new Dictionary<string, AbilityEntry>();
        MapAbilityStates();
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

    protected virtual void MapAbilityStates()
    {
        MapAttackDetailsToAbilityEntries();
    }

    private void MapAttackDetailsToAbilityEntries()
    {
        foreach (var attackDetail in attackDetails)
        {
            var entry = new AbilityEntry(
                name: attackDetail.name,
                animBoolName: attackDetail.animBoolName,
                state: null,
                minCooldown: attackDetail.minCooldown,
                maxCooldown: attackDetail.maxCooldown,
                chance: attackDetail.chance,
                rangeMin: attackDetail.rangeMin,
                rangeMax: attackDetail.rangeMax,
                attackAmount: attackDetail.attackAmount,
                attackTimeMin: attackDetail.attackTimeMin,
                attackTimeMax: attackDetail.attackTimeMax,
                action: attackDetail.action,
                unlocked: attackDetail.unlocked
            );

            abilityMap.Add(attackDetail.name, entry);
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

    public virtual void AssignLastAnimName(string _animBoolName) => lastAnimBoolName = _animBoolName;

    public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {
        moveSpeed = moveSpeed * (1 - _slowPercentage);
        anim.speed = anim.speed * (1 - _slowPercentage);

        Invoke("ReturnDefaultSpeed", _slowDuration);
    }

    protected override void ReturnDefaultSpeed()
    {
        base.ReturnDefaultSpeed();

        moveSpeed = defaultMoveSpeed;
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
        FreezeTime(true);

        yield return new WaitForSeconds(_seconds);
        FreezeTime(false);
    }

    public virtual void AnimationFinishTrigger() => stateMachine.currentState.AnimationFinishTrigger();
    public virtual void AnimationSpecialAttackTrigger()
    {

    }

    public virtual void AttackTrigger(string attackName, string checkLabel, string spawnName = null)
    {
        if (attackDetails == null || attackDetails.Count == 0) return;

        AttackDetail selectedDetail = null;
        foreach (var detail in attackDetails)
        {
            if (detail != null && detail.name == attackName)
            {
                selectedDetail = detail;
                break;
            }
        }
        if (selectedDetail == null) return;

        AttackCheck selectedCheck = null;
        if (selectedDetail.attackChecks != null)
        {
            foreach (var check in selectedDetail.attackChecks)
            {
                if (check != null && check.label == checkLabel)
                {
                    selectedCheck = check;
                    break;
                }
            }
        }
        if (selectedCheck == null || selectedCheck.checkTransform == null) return;

        if (selectedCheck.shape == AttackCheckShape.Point)
        {
            if (string.IsNullOrEmpty(spawnName) || selectedDetail.spawnPrefab == null) return;

            GameObject prefabToSpawn = null;
            foreach (var spawnSpec in selectedDetail.spawnPrefab)
            {
                if (spawnSpec != null && spawnSpec.name == spawnName)
                {
                    prefabToSpawn = spawnSpec.prefab;
                    break;
                }
            }
            if (prefabToSpawn == null) return;

            SpawnAttack(prefabToSpawn, selectedCheck.checkTransform.position);
            return;
        }

        ResolveCollision(selectedCheck);
    }

    private void SpawnAttack(GameObject prefab, Vector3 position)
    {
        Instantiate(prefab, position, Quaternion.identity);
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

    public virtual void SoundTrigger(string attackName, int soundIndex)
    {
        if (attackDetails == null || attackDetails.Count == 0) return;

        AttackDetail selectedDetail = null;
        foreach (var detail in attackDetails)
        {
            if (detail != null && detail.name == attackName)
            {
                selectedDetail = detail;
                break;
            }
        }
        if (selectedDetail == null || selectedDetail.sounds == null) return;
        if (soundIndex < 0 || soundIndex >= selectedDetail.sounds.Length) return;

        // Play the requested sound at/with this enemy's transform context
        StateSound sound = selectedDetail.sounds[soundIndex];
        sound.Play(transform);
    }

    public virtual void SelfDestroy()
    {

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

            float minR = Mathf.Max(0f, evadeRangeMin);
            float maxR = Mathf.Max(minR, evadeRangeMax);
            if (maxR <= minR) maxR = minR + stubLength;

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




    public virtual bool CanBeStunned()
    {
        return false;
    }
    public virtual void OpenCounterAttackWindow()
    {
    }

    public virtual void CloseCounterAttackWindow()
    {
    }

    public bool IsSummon()
    {
        return isSummon;
    }

    #region Battle States

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
