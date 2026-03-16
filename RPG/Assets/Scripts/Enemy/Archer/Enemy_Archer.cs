using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Archer : Enemy_Regular
{
    #region old code
    [Header("Archer specific info old")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed;

    #endregion

    #region Jump Ability Settings
    [Header("Jump Ability Settings")]
    [SerializeField] private float jumpRangeMin = 0f;
    [SerializeField] private float jumpRangeMax = 2f;
    [SerializeField] private float jumpCooldownMin = 1f;
    [SerializeField] private float jumpCooldownMax = 2f;
    [SerializeField] private float jumpChance = 1f;
    [SerializeField] private Vector2 jumpAbilityVelocity = new Vector2(8f, 12f);
    [SerializeField] private bool jumpBack = true;
    #endregion

    #region States
    public IdleStateBase<Enemy_Archer> idleState { get; private set; }
    public MoveStateBase<Enemy_Archer> moveState { get; private set; }
    public BattleStateBase<Enemy_Archer> battleState { get; private set; }
    public AttackStateBase<Enemy_Archer> attackState { get; private set; }
    public StunnedStateBase<Enemy_Archer> stunnedState { get; private set; }
    public DeadStateBase<Enemy_Archer> deadState { get; private set; }
    public JumpStateBase<Enemy_Archer> jumpState { get; private set; }
    public EvasionStateBase<Enemy_Archer> evasionState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        BuildStates();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    private void BuildStates()
    {
        idleState = new IdleWithTargets(
            this,
            stateMachine,
            "Idle",
            moveFactory: () => moveState,
            battleFactory: () => battleState
        );

        moveState = new MoveStateBase<Enemy_Archer>(
            this,
            stateMachine,
            "Move",
            idleState: () => idleState,
            battleState: () => battleState
        );

        battleState = new BattleStateBase<Enemy_Archer>(
            this,
            stateMachine,
            "Battle",
            idleState: () => idleState,
            nextState: () => idleState
        );

        AttackDetail detailAttack = null;
        if (attackDetails != null && attackDetails.Count > 0)
            detailAttack = attackDetails.Find(d => d != null && d.name == "Attack") ?? attackDetails[0];

        if (detailAttack != null)
        {
            attackState = new AttackStateBase<Enemy_Archer>(
                this,
                stateMachine,
                detailAttack,
                nextStateFactory: () => battleState
            );
        }

        stunnedState = new StunnedStateBase<Enemy_Archer>(
            this,
            stateMachine,
            "Stunned",
            nextState: () => battleState
        );

        deadState = new DeadStateBase<Enemy_Archer>(
            this,
            stateMachine,
            "Die",
            enterSounds: new List<StateSound>
            {
                new StateSound { name = "ArcherDie", useTransform = true }
            },
            exitSounds: null
        );

        jumpState = new JumpStateBase<Enemy_Archer>(
            this,
            stateMachine,
            "Jump",
            enterSounds: new List<StateSound>
            {
                new StateSound { name = "ArcherJump", useTransform = true }
            },
            exitSounds: null
        );

        evasionState = new EvasionStateBase<Enemy_Archer>(
            this,
            stateMachine,
            "Move",
            battleState: () => battleState
        );

        if (abilityMap != null)
        {
            if (abilityMap.TryGetValue("Attack", out var attack) && attack != null)
                attack.state = attackState;

            if (abilityMap.TryGetValue("Evade", out var evade) && evade != null)
            {
                evade.state = evasionState;
                evade.action = BattleAction.Evade;
                evade.unlocked = true;
            }

            if (abilityMap.TryGetValue("Jump", out var jump) && jump != null)
            {
                jump.state = jumpState;
                jump.action = BattleAction.Jump;
                jump.unlocked = true;
                jump.jumpVelocity = jumpAbilityVelocity;
                jump.jumpBack = jumpBack;
            }
        }
    }

    protected override void MapAbilityStates()
    {
        base.MapAbilityStates();

        if (abilityMap.TryGetValue("Attack", out var attack) && attack != null)
            attack.state = attackState;

        abilityMap["Evade"] = new AbilityEntry(
            name: "Evade",
            animBoolName: "Move",
            state: evasionState,
            minCooldown: evasionCooldownMin,
            maxCooldown: evasionCooldownMax,
            action: BattleAction.Evade,
            rangeMin: evadeRangeMin,
            rangeMax: evadeRangeMax,
            chance: 1f
        );

        abilityMap["Jump"] = new AbilityEntry(
            name: "Jump",
            animBoolName: "Jump",
            state: jumpState,
            minCooldown: jumpCooldownMin,
            maxCooldown: jumpCooldownMax,
            action: BattleAction.Jump,
            rangeMin: jumpRangeMin,
            rangeMax: jumpRangeMax,
            chance: jumpChance,
            jumpVelocity: jumpAbilityVelocity,
            jumpBack: jumpBack
        );
    }

    private sealed class IdleWithTargets : IdleStateBase<Enemy_Archer>
    {
        private readonly System.Func<EnemyState> moveFactory;
        private readonly System.Func<EnemyState> battleFactory;

        public IdleWithTargets(
            Enemy_Archer enemy,
            EnemyStateMachine sm,
            string animBool,
            System.Func<EnemyState> moveFactory,
            System.Func<EnemyState> battleFactory,
            List<StateSound> enterSounds = null,
            List<StateSound> exitSounds = null
        ) : base(enemy, sm, animBool, enterSounds, exitSounds)
        {
            this.moveFactory = moveFactory;
            this.battleFactory = battleFactory;
        }

        protected override EnemyState MoveState => moveFactory?.Invoke();
        protected override EnemyState BattleState => battleFactory?.Invoke();
    }

    public override bool CanBeStunned()
    {
        if (base.CanBeStunned())
        {
            stateMachine.ChangeState(stunnedState);
            return true;
        }
        return false;
    }

    public override void Die()
    {
        base.Die();
        if (stats.isDeadZone)
        {
            deadState = new DeadStateBase<Enemy_Archer>(this, stateMachine, "Idle");
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void AnimationSpecialAttackTrigger()
    {
        GameObject newArrow = Instantiate(arrowPrefab, attackCheck.position,Quaternion.identity);
        Transform player = PlayerManager.instance.player.transform;
        Vector3 direction = player.position - wallCheck.position;
        newArrow.GetComponent<Arrow_Controller>().SetupArrow(arrowSpeed * facingDir, stats, player.position, direction);
        AudioManager.instance.PlaySFX("ArcherAttack", transform);
    }

}
