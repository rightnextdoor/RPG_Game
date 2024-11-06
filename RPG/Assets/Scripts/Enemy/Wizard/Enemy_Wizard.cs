using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Wizard : Enemy_Boss
{
    [Header("Spell info")]
    [SerializeField] private float spellCooldown = 1.5f;
    private float spellCoolDownTimer = 0;
    [Space]
    #region Fire ball
    [Header("Fire ball")]
    [SerializeField] private GameObject fireBallPrefab;
    [SerializeField] private float fireBallSpeed = 6f;
    [SerializeField] private float fireBallExplosionTimer = 3f;
    public float fireBallCooldown = 10f;
    [HideInInspector] public float lastTimeCastFireBall;
    [HideInInspector] public bool isFireBall;
    #endregion

    #region Ice ball
    [Header("Ice ball")]
    [SerializeField] private Transform iceAttackCheck;
    [SerializeField] private float iceAttackCheckRadius = 1.2f;
    [SerializeField] private GameObject iceBallPrefab;
    [SerializeField] private float iceBallSpeed = 10f;
    [SerializeField] private float iceBallMaxSize = 10f;
    [SerializeField] private float iceBallGrowSpeed = 6;
    [SerializeField] private float iceBallExplosionTimer = 3f;
    public float iceBallCooldown = 5f;
    [HideInInspector] public float lastTimeCastIceBall;
    [HideInInspector] public bool isIceBall;
    #endregion

    #region Lighting strike
    [Header("Lighting strike")]
    public float lightingStrikeCooldown = 15f;
    [HideInInspector] public float lastTimeCastLightingStrike;
    #endregion

    #region Air walk
    [Header("Air walk")]
    public float airWalkCooldown = 18f;
    [HideInInspector] public float lastTimeCastAirWalk;
    #endregion

    #region States
    public Wizard_IdleState idleState { get; private set; }
    public Wizard_BattleState battleState { get; private set; }
    public Wizard_AttackState attackState { get; private set; }
    public Wizard_DeadState deadState { get; private set; }
    public Wizard_StartState startState { get; private set; }
    public Wizard_MoveState moveState { get; private set; }
    public Wizard_TeleportState teleportState { get; private set; }
    public Wizard_SpellState spellState { get; private set; }
    public Wizard_AirWalkState airWalkState { get; private set; }
    public Wizard_FireBallState fireBallState { get; private set; }
    public Wizard_IceBallState iceBallState { get; private set; }
    public Wizard_LightingStrikeState lightingStrikeState { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new Wizard_IdleState(this, stateMachine, "Idle", this);
        battleState = new Wizard_BattleState(this, stateMachine, "Battle", this);
        attackState = new Wizard_AttackState(this, stateMachine, "Attack", this);
        deadState = new Wizard_DeadState(this, stateMachine, "Die", this);
        startState = new Wizard_StartState(this, stateMachine, "Idle", this);
        moveState = new Wizard_MoveState(this, stateMachine, "Move", this);
        teleportState = new Wizard_TeleportState(this, stateMachine, "Teleport", this);
        spellState = new Wizard_SpellState(this, stateMachine, "Idle", this);
        airWalkState = new Wizard_AirWalkState(this, stateMachine, "Idle", this);
        fireBallState = new Wizard_FireBallState(this, stateMachine, "FireBall", this);
        iceBallState = new Wizard_IceBallState(this, stateMachine, "IceBall", this);
        lightingStrikeState = new Wizard_LightingStrikeState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
    }

    protected override void Update()
    {
        base.Update();  
        spellCoolDownTimer -= Time.deltaTime;
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new Wizard_DeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.DrawWireSphere(iceAttackCheck.position, iceAttackCheckRadius);
    }

    public bool CanCastSpell()
    {
        if (spellCoolDownTimer < 0)
        {
            spellCoolDownTimer = spellCooldown;
            return true;
        }
        return false;
    }

    public override void AnimationSpecialAttackTrigger()
    {
        if (isFireBall)
        {
            CastFireBall();
        }

        if (isIceBall)
        {
            CastIceBall();
        }
    }

    private void CastFireBall()
    {
        GameObject castFireBall = Instantiate(fireBallPrefab, attackCheck.position, Quaternion.identity);
        castFireBall.GetComponent<WizardFireBall_Controller>().SetupFireBall(fireBallSpeed * facingDir, stats, attackCheckRadius, fireBallExplosionTimer);
        isFireBall = false;
    }

    private void CastIceBall()
    {
        GameObject castIceBall = Instantiate(iceBallPrefab, iceAttackCheck.position, Quaternion.identity);
        castIceBall.GetComponent<WizardIceBall_Controller>().SetupIceBall(iceBallSpeed, stats, iceBallExplosionTimer,iceBallGrowSpeed, iceBallMaxSize, iceAttackCheckRadius);
        isIceBall = false;
    }
}
