using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Wizard : Enemy_Boss
{
    public float defaultGravity;
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
    [SerializeField] private GameObject lightingStrikePrefab;
    public float lightingStrikeCooldown = 15f;
    [HideInInspector] public bool isLightingStrike;
    [HideInInspector] public float lastTimeCastLightingStrike;
    #endregion

    #region Air walk
    [Header("Air walk")]
    [SerializeField] private GameObject airWalkAttackPrefab;
    public float airWalkCooldown = 18f;
    [SerializeField] float airWalkCastTimer = .5f;
    [SerializeField] int numberOfAirWalkAttack = 4;
    public Transform leftPosition;
    public Transform rightPosition;
    [HideInInspector] public Vector3 moveToPosition;
    [HideInInspector] public bool isAirwalking;
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
        airWalkState = new Wizard_AirWalkState(this, stateMachine, "AirWalk", this);
        fireBallState = new Wizard_FireBallState(this, stateMachine, "FireBall", this);
        iceBallState = new Wizard_IceBallState(this, stateMachine, "IceBall", this);
        lightingStrikeState = new Wizard_LightingStrikeState(this, stateMachine, "Lighting", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
        defaultGravity = rb.gravityScale;
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
        Debug.Log("attack trigger");
        if (isFireBall)
        {
            CastFireBall();
        }

        if (isIceBall)
        {
            CastIceBall();
        }

        if (isLightingStrike)
        {
            CastLightingStrike();
        }

        if (isAirwalking)
        {
            CastAirWalk();
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
    #region Lighting Strike
    private void CastLightingStrike()
    {
        List<GameObject> leftLightStrike = new List<GameObject>();
        List<GameObject> rightLightStrike = new List<GameObject>();
        
        CreateLightingStrike(leftLightStrike, rightLightStrike);

        foreach (GameObject lighting in leftLightStrike)
        {
            lighting.GetComponent<WizardLightingStrike_Controller>().SetupLightingStrike(stats);
        }

        foreach (GameObject lighting in rightLightStrike)
        {
            lighting.GetComponent<WizardLightingStrike_Controller>().SetupLightingStrike(stats);
        }

        isLightingStrike = false;
    }

    private void CreateLightingStrike(List<GameObject> leftLightStrike, List<GameObject> rightLightStrike)
    {
        int numberOfLighting = 7;

        Vector3 xOffSet = new Vector3(-3, 0);
        Vector3 strikePosition = transform.position + new Vector3(-1, 0);

        SetupLightingStrike(leftLightStrike, xOffSet, ref strikePosition, numberOfLighting);

        xOffSet = new Vector3(3, 0);
        strikePosition = transform.position + new Vector3(1, 0);

        SetupLightingStrike(rightLightStrike, xOffSet, ref strikePosition, numberOfLighting);
    }

    private void SetupLightingStrike(List<GameObject> lightStrike, Vector3 xOffSet, ref Vector3 strikePosition, int numberOfLighting)
    {
        GameObject castLightingStrike;
        for (int i = 0; i < numberOfLighting; i++)
        {
            castLightingStrike = Instantiate(lightingStrikePrefab, strikePosition, Quaternion.identity);
            lightStrike.Add(castLightingStrike);
            strikePosition += xOffSet;
        }
    }
    #endregion

    public override void MoveToPosition()
    {
        transform.position = moveToPosition;
    }

    private void CastAirWalk()
    {
        isAirwalking = false;
        StartCoroutine(AirWalkAttack());
                
        
    }

    IEnumerator AirWalkAttack()
    {
        int spriteSelected;
        for (int i = 0; i < numberOfAirWalkAttack; i++)
        {
            if (i % 2 == 0)
            {
                spriteSelected = 0;
            } else
            {
                spriteSelected = 1;
            }

            Vector3 attackPosition = transform.position + new Vector3(0, -4);
            GameObject castAirWalkAttack = Instantiate(airWalkAttackPrefab, attackPosition, Quaternion.identity);
            castAirWalkAttack.GetComponent<WizardAirWalkAttack_Controller>().SetupAirWalkAttack(stats,spriteSelected);

            yield return new WaitForSeconds(airWalkCastTimer);
        }
        
    }
}
