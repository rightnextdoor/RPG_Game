using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }

    public SpriteRenderer sr { get; private set; }
    public CharacterStats stats { get; private set; }
    public CapsuleCollider2D cd { get; private set; }
    #endregion

    [Header("Knockback info")]
    [SerializeField] protected Vector2 knockbackPower = new Vector2(7, 12);
    [SerializeField] protected Vector2 knockbackOffset = new Vector2(.5f, 2);
    [SerializeField] protected float knockbackDuration = .07f;
    protected bool isKnocked;

    [Header("Collision info")]
    public Transform attackCheck;
    public float attackCheckRadius = 1.2f;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundCheckDistance = 1;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected float wallCheckDistance = .8f;
    [SerializeField] protected LayerMask whatIsGround;
    [HideInInspector] public bool boundaryTouchedOnMove;

    public int KnockbackDir { get; private set; }
    public int facingDir { get; private set; } = 1;
    protected bool facingRight = true;

    private RigidbodyConstraints2D defautConstraints;

    public System.Action onFlipped;

    protected virtual void Awake()
    {
        SetupDefaultFacingDir(ComputeFacingFromTransform());
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        SetupDefaultFacingDir(ComputeFacingFromTransform());
    }
#endif

    private int ComputeFacingFromTransform()
    {
        float y = transform.eulerAngles.y;

        float dy = Mathf.DeltaAngle(y, 0f);
        return (Mathf.Abs(dy) <= 90f) ? 1 : -1;
    }

    public virtual void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {

    }

    protected virtual void ReturnDefaultSpeed()
    {
        anim.speed = 1;
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<CharacterStats>();
        cd = GetComponent<CapsuleCollider2D>();

        defautConstraints = rb.constraints;
    }

    protected virtual void Update()
    {
    }

    public void StopPlayer()
    {
        SetZeroVelocity();
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    public void ReturnPlayerMove()
    {
        rb.constraints = defautConstraints;
    }

    public virtual void DamageImpact() => StartCoroutine("HitKnockback");

    public virtual void SetupKnockbackDir(Transform _damageDirection)
    {
        if (_damageDirection.position.x > transform.position.x)
            KnockbackDir = -1;
        else if (_damageDirection.position.x < transform.position.x)
            KnockbackDir = 1;
    }

    public void SetupKnockbackPower(Vector2 _knockbackPower) => knockbackPower = _knockbackPower;

    protected virtual IEnumerator HitKnockback()
    {
        isKnocked = true;

        float xOffset = Random.Range(knockbackOffset.x, knockbackOffset.y);

        if (knockbackPower.x > 0 || knockbackPower.y > 0) //comment out if you want player to freeze
            rb.linearVelocity = new Vector2((knockbackPower.x + xOffset) * KnockbackDir, knockbackPower.y);

        yield return new WaitForSeconds(knockbackDuration);
        isKnocked = false;
        SetupZeroKnockbackPower();
    }

    protected virtual void SetupZeroKnockbackPower()
    {

    }

    #region Velocity
    public void SetZeroVelocity()
    {
        if (isKnocked)
            return;

        rb.linearVelocity = new Vector2(0, 0);
    }

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        if (isKnocked)
            return;

        rb.linearVelocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }
    #endregion

    #region Collision
    public Transform GetGroundCheck() => groundCheck;
    public Transform GetWallCheck() => wallCheck;
    public float GetGroundCheckDistance() => groundCheckDistance;
    public float GetWallCheckDistance() => wallCheckDistance;
    public LayerMask GetWhatIsGround() => whatIsGround;

    private Vector2 OppositeX(Transform probe)
    {
        Vector2 entityPos = transform.position;
        Vector2 probePos = probe.position;
        return new Vector2(2f * entityPos.x - probePos.x, probePos.y);
    }


    public virtual bool IsGroundDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    public virtual bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    public virtual bool IsGroundBehindDetected() => Physics2D.Raycast(OppositeX(groundCheck), Vector2.down, groundCheckDistance, whatIsGround);
    public virtual bool IsWallBehindDetected() => Physics2D.Raycast(OppositeX(wallCheck), Vector2.right * facingDir, wallCheckDistance, whatIsGround);


    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        var g = GetGroundCheck();
        var w = GetWallCheck();
        if (g != null)
        {
            Vector3 start = g.position;
            Vector3 end = start + (-transform.up) * GetGroundCheckDistance();
            Gizmos.DrawLine(start, end);
        }
        if (w != null)
        {
            Vector3 start = w.position;
            Vector3 end = start + (transform.right) * GetWallCheckDistance();
            Gizmos.DrawLine(start, end);
        }
#endif
    }
    #endregion

    #region Flip
    public virtual void Flip()
    {
        facingDir = facingDir * -1;
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);

        if (onFlipped != null)
            onFlipped();
    }

    public virtual void FlipController(float _x)
    {
        if (_x > 0 && !facingRight)
            Flip();
        else if (_x < 0 && facingRight)
            Flip();
    }

    public virtual void SetupDefaultFacingDir(int _direction)
    {
        facingDir = _direction;
        if (facingDir == -1) facingRight = false;
        else if (facingDir == 1) facingRight = true;
    }
    #endregion

    public bool IsFacingRight()
    {
        return facingRight;
    }


    public virtual void Die()
    {

    }
}
