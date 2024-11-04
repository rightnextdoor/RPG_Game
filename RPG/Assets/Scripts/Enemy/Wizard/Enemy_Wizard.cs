using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Wizard : Enemy_Boss
{
    [Header("Spell info")]
    [SerializeField] private float spellCooldown = 1.5f;
    private float spellCoolDownTimer = 0;
    [Space]
    [Header("Fire ball")]
    [SerializeField] private float fireBallCooldown = 10f;
    public float lastTimeCastFireBall;

    [Header("Ice ball")]
    [SerializeField] private float iceBallCooldown = 5f;
    public float lastTimeCastIceBall;

    [Header("Lighting strike")]
    [SerializeField] private float lightingStrikeCooldown = 15f;
    public float lastTimeCastLightingStrike;

    [Header("Air walk")]
    [SerializeField] private float airWalkCooldown = 18f;
    public float lastTimeCastAirWalk;

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
        fireBallState = new Wizard_FireBallState(this, stateMachine, "Idle", this);
        iceBallState = new Wizard_IceBallState(this, stateMachine, "Idle", this);
        lightingStrikeState = new Wizard_LightingStrikeState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);

        SetupSpellsCooldown();
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

    private void SetupSpellsCooldown()
    {
        lastTimeCastFireBall = Time.time;
        lastTimeCastIceBall = Time.time;
        lastTimeCastLightingStrike = Time.time;
        lastTimeCastAirWalk = Time.time;
    }

    public bool CanCastSpell()
    {
        if (spellCoolDownTimer < 0)
        {
            if (CastAirWalk())
            {
                stateMachine.ChangeState(airWalkState);
                spellCoolDownTimer = spellCooldown;
                return true;
            }

            if (CastLightingStrike())
            {
                stateMachine.ChangeState(lightingStrikeState);
                spellCoolDownTimer = spellCooldown;
                return true;
            }

            if (CastIceBall())
            {
                stateMachine.ChangeState(iceBallState);
                spellCoolDownTimer = spellCooldown;
                return true;
            }

            if (CastFireBall())
            {
                stateMachine.ChangeState(fireBallState);
                spellCoolDownTimer = spellCooldown;
                return true;
            }
        }
        
        return false;
    }

    private bool CastFireBall()
    {
        if (Time.time >= lastTimeCastFireBall + fireBallCooldown)
        {
            return true;
        }
        return false;
    }

    private bool CastIceBall()
    {
        if (Time.time >= lastTimeCastIceBall + iceBallCooldown)
        {
            return true;
        }
        return false;
    }

    private bool CastLightingStrike()
    {
        if (Time.time >= lastTimeCastLightingStrike + lightingStrikeCooldown)
        {
            return true;
        }
        return false;
    }

    private bool CastAirWalk()
    {
        if (Time.time >= lastTimeCastAirWalk + airWalkCooldown)
        {
            return true;
        }
        return false;
    }
}
