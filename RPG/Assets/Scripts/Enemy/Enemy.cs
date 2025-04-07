using System.Collections;
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

    [Header("Move info")]
    public bool canPatrol = true;
    public float moveSpeed = 1.5f;  
    public float idleTime = 2;
    public float moveTime = 5;
    public float battleTime= 7;
    private float defaultMoveSpeed;

    [Header("Range Attack info")]
    public float agroDistance = 2;
    public float rangeAttackDistance = 2;
    [HideInInspector] public float rangeAttackCooldown;
    public float minRangeAttackCooldown = 1;
    public float maxRangeAttackCooldown = 2;
    [HideInInspector] public float lastTimeRangeAttacked;
    
    [Header("Melee Attack info")]
    public float meleeAttackDistance = 2;
    [HideInInspector] public float meleeAttackCooldown;
    public float minMeleeAttackCooldown = 1;
    public float maxMeleeAttackCooldown = 2;
    [HideInInspector] public float lastTimeMeleeAttacked;

    [Header("Evasion info")]
    public float evasionTimer = 1.5f;
    public float evasionSpeed = 10f;
    [SerializeField] private float evasionDistance = 1f; //How close player should be to trigger before evading
    [SerializeField] private float minEvasionCooldown = 0f;
    [SerializeField] private float maxEvasionCooldown = .5f;
    private float evasionCooldown;
    private float lastTimeEvade;

    [HideInInspector] public bool isSummon = false;

    public EnemyStateMachine stateMachine { get; private set; }
    public EntityFX fX { get; private set; }
    public string lastAnimBoolName {  get; private set; }

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new EnemyStateMachine();

        defaultMoveSpeed = moveSpeed;
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

    public virtual void AttackTrigger()
    {    
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, attackCheckRadius);

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                PlayerStats target = hit.GetComponent<PlayerStats>();
                stats.DoDamage(target);
            }
        }
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

        Gizmos.color = Color.red;
        Gizmos.DrawLine(attackCheck.position, new Vector3(attackCheck.position.x + playerDistance * facingDir, attackCheck.position.y));
        
        Gizmos.color = Color.blue;
        Vector3 offset = new Vector3(.3f, .3f);
        Vector3 meleePosition = transform.position + offset;
        Gizmos.DrawLine(meleePosition, new Vector3(meleePosition.x + meleeAttackDistance * facingDir, meleePosition.y));

        Gizmos.color = Color.green;
        offset = new Vector3(0, .7f);
        Vector3 evasionPosition = transform.position + offset;
        Gizmos.DrawLine(evasionPosition, new Vector3(evasionPosition.x + evasionDistance * facingDir, evasionPosition.y));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x + rangeAttackDistance * facingDir, transform.position.y));
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
            
            SetVelocity(moveSpeed * facingDir, rb.velocity.y);
        }

        if (IsPlayerDetected().distance <= rangeAttackDistance)
        {
            if (player != null)
            {
                if (Vector2.Distance(player.transform.position, transform.position) < evasionDistance)
                {
                    if (evasionState != null)
                    {
                        if(CanEvade())
                            stateMachine.ChangeState(evasionState);
                    }
                        
                }
            }

            if (CanRangeAttack())
            {
                if(attackState != null)
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

            SetVelocity(moveSpeed * facingDir, rb.velocity.y);

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
            
            SetVelocity(moveSpeed * facingDir, rb.velocity.y);

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
        if (Time.time >= lastTimeEvade + evasionCooldown)
        {
            evasionCooldown = Random.Range(minEvasionCooldown, maxEvasionCooldown);
            lastTimeEvade = Time.time;
            return true;
        }
        return false;
    }

    public void RunIntoPlayerAttack(EnemyState moveState)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, attackCheckRadius);
        bool hitPlayer = false;
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                PlayerStats target = hit.GetComponent<PlayerStats>();
                if (!hitPlayer)
                {
                    stats.DoDamage(target);
                    hitPlayer = true;
                    if(moveState != null)
                        stateMachine.ChangeState(moveState);
                }
            }
        }
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
