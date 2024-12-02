using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Jumper : Enemy_Regular
{
    [Header("Jumper info")]
    public float hitTimer = 1f;
    [HideInInspector] public float hitCountdown;

    public Vector2 jumpVelocity;
    public float jumpCooldown;
    public float safeDistance;
    [HideInInspector] public float lastTimeJumped;

    [Header("Additional collision check")]
    [SerializeField] private Transform jumpCheck;
    [SerializeField] private Vector2 jumpCheckSize;

    #region States
    public JumperIdleState idleState { get; private set; }
    public JumperJumpState jumpState { get; private set; }
    public JumperBattleState battleState { get; private set; }
    public JumperDeadState deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new JumperIdleState(this, stateMachine, "Idle", this);
        jumpState = new JumperJumpState(this, stateMachine, "Jump", this);
        battleState = new JumperBattleState(this, stateMachine, "Idle", this);
        deadState = new JumperDeadState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();
        hitCountdown -= Time.deltaTime;
    }

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }

    public bool GroundBehind() => Physics2D.BoxCast(jumpCheck.position, jumpCheckSize, 0, Vector2.zero, 0, whatIsGround);
    public bool WallBehind() => Physics2D.Raycast(wallCheck.position, Vector2.right * -facingDir, wallCheckDistance + 2, whatIsGround);

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.DrawWireCube(jumpCheck.position, jumpCheckSize);
    }

    public void Attack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, attackCheckRadius);

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                PlayerStats target = hit.GetComponent<PlayerStats>();
                if (hitCountdown < 0f)
                {
                    stats.DoDamage(target);
                    //ToDo: knock the player back when hit
                    hitCountdown = hitTimer;
                }

            }
        }
    }
}
