using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity
{
    [Header("Attack details")]
    public Vector2[] attackMovement;
    public float counterAttackDuration = .2f;

    public bool isBusy {  get; private set; }
    [Header("Move info")]
    public float moveSpeed = 12f;
    public float jumpForce;
    public float swordReturnImpact = 2;
    private float defaultMoveSpeed;
    private float defaultJumpForce;

    [Header("Dash info")]
    public float dashSpeed;
    public float dashDuration;
    private float defaultDashSpeed;
    public bool canDash;

    [HideInInspector] public bool canDoubleJump;

    [HideInInspector] public bool canWallSlide;

    public float dashDir {  get; private set; }

    public SkillManager skill {  get; private set; }
    public GameObject sword { get; private set; }
    public PlayerFX fX { get; private set; }

    [HideInInspector] public bool transtionUp;
    [HideInInspector] public bool transtionDown;

    private bool canControl = true;
    public static event System.Action<Player> OnPlayerSpawned;
    #region States
    public PlayerStateMachine stateMachine { get; private set; }

    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerWallSlideState wallSlide { get; private set; }
    public PlayerWallJumpState wallJump { get; private set; }
    public PlayerDashState dashState { get; private set; }

    public PlayerPrimaryAttackState primaryAttack { get; private set; }
    public PlayerCounterAttackState counterAttack { get; private set; }
    public PlayerAimSwordState aimSowrd { get; private set; }
    public PlayerCatchSwordState catchSword { get; private set; }
    public PlayerBlackholeState blackHole { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    public PlayerLevelTransitionState levelTransition { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        wallSlide = new PlayerWallSlideState(this, stateMachine, "WallSlide");
        wallJump = new PlayerWallJumpState(this, stateMachine, "Jump");

        primaryAttack = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
        counterAttack = new PlayerCounterAttackState(this, stateMachine, "CounterAttack");
        aimSowrd = new PlayerAimSwordState(this, stateMachine, "AimSword");
        catchSword = new PlayerCatchSwordState(this, stateMachine, "CatchSword");
        blackHole = new PlayerBlackholeState(this, stateMachine, "Jump");

        deadState = new PlayerDeadState(this, stateMachine, "Die");
        levelTransition = new PlayerLevelTransitionState(this, stateMachine, "Move");
    }

    protected override void Start()
    {
        base.Start();

        fX = GetComponent<PlayerFX>();

        skill = SkillManager.instance;

        stateMachine.Initialize(idleState);

        defaultMoveSpeed = moveSpeed;
        defaultJumpForce = jumpForce;
        defaultDashSpeed = dashSpeed;

        canWallSlide = true;
    }

    protected override void Update()
    {
        if (Time.timeScale == 0) 
            return;

        if (!canControl) return;

        base.Update();

        stateMachine.currentState.Update();
        CheckForDashInput();

        if (Input.GetKeyDown(KeyCode.F) && skill.crystal.crystalUnlocked && !skill.inBlackholeState)
            skill.crystal.CanUseSkill();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            Inventory.instance.UseFlask();
     
    }

    private void OnEnable()
    {
        OnPlayerSpawned?.Invoke(this);
    }

    public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {
        moveSpeed = moveSpeed * (1 - _slowPercentage);
        jumpForce = jumpForce * (1 - _slowPercentage);
        dashSpeed = dashSpeed * (1 - _slowPercentage);
        anim.speed = anim.speed * (1 - _slowPercentage);

        Invoke("ReturnDefaultSpeed", _slowDuration);
    }

    protected override void ReturnDefaultSpeed()
    {
        base.ReturnDefaultSpeed();

        moveSpeed = defaultMoveSpeed;
        jumpForce = defaultJumpForce;
        dashSpeed = defaultDashSpeed;
    }

    public void AssignNewSword(GameObject _newSword)
    {
        sword = _newSword;
    }

    public void CatchTheSword()
    {
        stateMachine.ChangeState(catchSword);
        Destroy(sword);
    }

    public IEnumerator BusyFor(float _seconds)
    {
        isBusy = true;
        yield return new WaitForSeconds(_seconds);
        isBusy = false;
    }
    //TODO remove this method and level transition state when cutscene it done
    public void LevelTransition(bool _transitionDown, bool _transitionUP)
    {
        if (_transitionDown || _transitionUP)
        {
            levelTransition = new PlayerLevelTransitionState(this, stateMachine, "Jump");
        }
        transtionDown = _transitionDown;
        transtionUp = _transitionUP;

        stateMachine.ChangeState(levelTransition);
    }

    public void AnimationTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    private void CheckForDashInput()
    {
        if (IsWallDetected())
            return;

        if (skill.dash.dashUnlocked == false)
            return;

        if (!IsGroundDetected())
        {
            if (!canDash)
                return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && SkillManager.instance.dash.CanUseSkill())
        {
            if(!IsGroundDetected())
                canDash = false;

            dashDir = Input.GetAxisRaw("Horizontal");

            if (dashDir == 0)
                dashDir = facingDir;

            stateMachine.ChangeState(dashState);
        }
    }

    public override void Die()
    {      
        base.Die();
        if (stats.isDeadZone)
        {
            //deadState = new PlayerDeadState(this, stateMachine, "Idle");
            //anim.speed = 0;
            //cd.enabled = false;
            StartCoroutine(DelayDeath());
        }
        EnemyManager.instance.ResetEnemyDeath();
        stateMachine.ChangeState(deadState);
    }

    private IEnumerator DelayDeath()
    {
        yield return new WaitForSeconds(.1f);
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

    }

    protected override void SetupZeroKnockbackPower()
    {
        knockbackPower = new Vector2(0,0);
    }

    public void DisableControl() => canControl = false;
    public void EnableControl() => canControl = true;
}
