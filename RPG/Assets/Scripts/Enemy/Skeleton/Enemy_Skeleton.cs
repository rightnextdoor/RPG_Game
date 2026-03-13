using System.Collections.Generic;
using UnityEngine;

public class Enemy_Skeleton : Enemy_Regular
{
    [Header("Multi Attack old")]
    public bool HasMultiAttack;
    public bool IsSpearSkeleton;
    public float chanceToMultiAttack = 0;
    public float defaultChanceToMultiAttack = 5;
    [SerializeField] private float multiAttackCooldown = 5f;
    [HideInInspector] public float multiAttackCooldownTimer = 0;

    #region States
    public IdleStateBase<Enemy_Skeleton> idleState { get; private set; }
    public MoveStateBase<Enemy_Skeleton> moveState { get; private set; }
    public BattleStateBase<Enemy_Skeleton> battleState { get; private set; }
    public AttackStateBase<Enemy_Skeleton> attackState { get; private set; }
    public AttackStateBase<Enemy_Skeleton> attack2State { get; private set; }
    public StunnedStateBase<Enemy_Skeleton> stunnedState { get; private set; }
    public DeadStateBase<Enemy_Skeleton> deadState { get; private set; }
    public EvasionStateBase<Enemy_Skeleton> evasionState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        BuildStates();
    }



    protected override void Update()
    {
        base.Update();

        multiAttackCooldownTimer -= Time.deltaTime;
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    private void BuildStates()
    {
        idleState = new IdleWithTargets(
                    this, stateMachine, "Idle",
                    moveFactory: () => moveState,
                    battleFactory: () => battleState,
                    enterSounds: null,
                    exitSounds: new System.Collections.Generic.List<StateSound> {
                new StateSound { name = "SkeletonIdle", useTransform = true }
                    }
                );
        moveState = new MoveStateBase<Enemy_Skeleton>(
            this,
            stateMachine,
            "Move",
            idleState: () => idleState,
            battleState: () => battleState
        );
        battleState = new BattleStateBase<Enemy_Skeleton>(
            this,
            stateMachine,
            "Battle",
            idleState: () => idleState,
            nextState: () => idleState
        );
        AttackDetail detailAttack = null;
        AttackDetail detailAttack2 = null;

        if (attackDetails != null && attackDetails.Count > 0)
        {
            detailAttack = attackDetails.Find(d => d != null && d.name == "Attack")
                           ?? attackDetails[0];

            if (attackDetails.Count > 1)
                detailAttack2 = attackDetails.Find(d => d != null && d.name == "Attack2")
                                ?? attackDetails[1];
            else
                detailAttack2 = detailAttack;
        }
        if (detailAttack != null)
        {
            attackState = new AttackStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                detailAttack,
                nextStateFactory: () => battleState
            );
        }

        if (detailAttack2 != null)
        {
            attack2State = new AttackStateBase<Enemy_Skeleton>(
                this,
                stateMachine,
                detailAttack2,
                nextStateFactory: () => battleState
            );
        }
        stunnedState = new StunnedStateBase<Enemy_Skeleton>(
            this,
            stateMachine,
            "Stunned",
            nextState: () => battleState
        );
        deadState = new DeadStateBase<Enemy_Skeleton>(
            this,
            stateMachine,
            "Die",
            enterSounds: new List<StateSound> {
                new StateSound { name = "SkeletonDie", useTransform = true }
            },
            exitSounds: null
        );
        evasionState = new EvasionStateBase<Enemy_Skeleton>(
            this,
            stateMachine,
            "Move",
            battleState: () => battleState
        );

        if (abilityMap != null)
        {
            if (abilityMap.TryGetValue("Attack", out var a1) && a1 != null)
                a1.state = attackState;

            if (abilityMap.TryGetValue("Attack2", out var a2) && a2 != null)
                a2.state = attack2State;

            if (abilityMap.TryGetValue("Evade", out var mv) && mv != null)
            {
                mv.state = evasionState;
                mv.action = BattleAction.Evade;
                mv.unlocked = true;
            }
        }
    }

    protected override void MapAbilityStates()
    {
        base.MapAbilityStates();

        if (abilityMap.TryGetValue("Attack", out var a1) && a1 != null)
        {
            a1.state = attackState;
        }

        if (abilityMap.TryGetValue("Attack2", out var a2) && a2 != null)
        {
            a2.state = attack2State;
        }

        abilityMap["Evade"] = new AbilityEntry(
            name: "Evade",
            animBoolName: "Move",
            state: evasionState,
            rangeMin: evadeRangeMin,
            rangeMax: evadeRangeMax,
            minCooldown: evasionCooldownMin,
            maxCooldown: evasionCooldownMax,
            chance: 1f,
            action: BattleAction.Evade
        );
    }

    private sealed class IdleWithTargets : IdleStateBase<Enemy_Skeleton>
    {
        private readonly System.Func<EnemyState> moveFactory;
        private readonly System.Func<EnemyState> battleFactory;

        public IdleWithTargets(
            Enemy_Skeleton enemy,
            EnemyStateMachine sm,
            string animBool,
            System.Func<EnemyState> moveFactory,
            System.Func<EnemyState> battleFactory,
            System.Collections.Generic.List<StateSound> enterSounds = null,
            System.Collections.Generic.List<StateSound> exitSounds = null
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
            deadState = new DeadStateBase<Enemy_Skeleton>(this, stateMachine, "Idle");
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void SelfDestroy()
    {
        base.SelfDestroy();

        if (GetComponentInChildren<UI_HealthBar>() != null)
        {
            GetComponentInChildren<UI_HealthBar>().HidHealthBar();
        }

        Destroy(gameObject, 2f);
    }

    public bool CanMultiAttack()
    {
        if (multiAttackCooldownTimer < 0)
        {
            if (Random.Range(0, 100) >= chanceToMultiAttack)
            {
                chanceToMultiAttack = defaultChanceToMultiAttack;
                multiAttackCooldownTimer = multiAttackCooldown;
                return true;
            }
            return false;
        }
        return false;
    }


}
