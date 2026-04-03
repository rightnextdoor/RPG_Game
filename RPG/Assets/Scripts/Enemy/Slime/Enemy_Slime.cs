using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SlimeType
{
    big,
    medium,
    small
}

public class Enemy_Slime : Enemy_Regular
{
    [Header("Slime specific")]
    [SerializeField] private SlimeType slimeType;
    [SerializeField] private int slimesToCreate;
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Vector2 minCreationVelocity;
    [SerializeField] private Vector2 maxCreationVelocity;

    public CooldownSystem cooldownSystem { get; private set; }
    public AbilityHub abilityHub { get; private set; }
    #region
    public IdleStateBase<Enemy_Slime> idleState { get; private set; }
    public MoveStateBase<Enemy_Slime> moveState { get; private set; }
    public BattleStateBase<Enemy_Slime> battleState { get; private set; }
    public AttackStateBase<Enemy_Slime> attackState { get; private set; }
    public StunnedStateBase<Enemy_Slime> stunnedState { get; private set; }
    public DeadStateBase<Enemy_Slime> deadState { get; private set; }
    public EvasionStateBase<Enemy_Slime> evasionState { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        //SetupDefaultFacingDir(-1);
        SetupStates();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    #region Setup
    private void SetupStates()
    {
        BuildStates();
        MapAbilityStates();

        cooldownSystem = new CooldownSystem();
        cooldownSystem.Setup(abilityMap, battleMinCooldown, battleMaxCooldown);

        abilityHub = new AbilityHub();
        abilityHub.BuildAbilityLists(abilityMap);
        abilityHub.Setup(cooldownSystem);

        battleState?.Configure(abilityHub, cooldownSystem, abilityMap, battlePreference);
    }

    private void BuildStates()
    {
        StateDetail idleDetail = null;
        StateDetail moveDetail = null;
        StateDetail battleDetail = null;
        StateDetail attack1Detail = null;
        StateDetail stunnedDetail = null;
        StateDetail deadDetail = null;
        StateDetail evasionDetail = null;

        AssignStateDetails(EnemyStateType.Idle, detail => idleDetail = detail);
        AssignStateDetails(EnemyStateType.Move, detail => moveDetail = detail);
        AssignStateDetails(EnemyStateType.Battle, detail => battleDetail = detail);
        AssignStateDetails(EnemyStateType.Attack, detail => attack1Detail = detail);
        AssignStateDetails(EnemyStateType.Stunned, detail => stunnedDetail = detail);
        AssignStateDetails(EnemyStateType.Dead, detail => deadDetail = detail);
        AssignStateDetails(EnemyStateType.Evasion, detail => evasionDetail = detail);

        if (idleDetail != null)
        {
            idleState = new IdleWithTargets(
                this,
                stateMachine,
                idleDetail.animBoolName,
                moveFactory: () => moveState,
                battleFactory: () => battleState,
                enterSounds: ToSoundList(idleDetail.enterSounds),
                exitSounds: ToSoundList(idleDetail.exitSounds)
            );
        }

        if (moveDetail != null)
        {
            moveState = new MoveStateBase<Enemy_Slime>(
                this,
                stateMachine,
                moveDetail.animBoolName,
                idleState: () => idleState,
                battleState: () => battleState,
                enterSounds: ToSoundList(moveDetail.enterSounds),
                exitSounds: ToSoundList(moveDetail.exitSounds)
            );
        }

        if (battleDetail != null)
        {
            battleState = new BattleStateBase<Enemy_Slime>(
                this,
                stateMachine,
                battleDetail.animBoolName,
                idleState: () => idleState,
                nextState: () => idleState,
                enterSounds: ToSoundList(battleDetail.enterSounds),
                exitSounds: ToSoundList(battleDetail.exitSounds)
            );
        }

        if (attack1Detail != null)
        {
            attackState = new AttackStateBase<Enemy_Slime>(
                this,
                stateMachine,
                attack1Detail.animBoolName,
                nextStateFactory: () => battleState,
                enterSounds: ToSoundList(attack1Detail.enterSounds),
                exitSounds: ToSoundList(attack1Detail.exitSounds)
            );
        }

        if (stunnedDetail != null)
        {
            stunnedState = new StunnedStateBase<Enemy_Slime>(
                this,
                stateMachine,
                stunnedDetail.animBoolName,
                nextState: () => battleState,
                enterSounds: ToSoundList(stunnedDetail.enterSounds),
                exitSounds: ToSoundList(stunnedDetail.exitSounds)
            );
        }

        if (deadDetail != null)
        {
            deadState = new DeadStateBase<Enemy_Slime>(
                this,
                stateMachine,
                deadDetail.animBoolName,
                enterSounds: ToSoundList(deadDetail.enterSounds),
                exitSounds: ToSoundList(deadDetail.exitSounds)
            );
        }

        if (evasionDetail != null)
        {
            evasionState = new EvasionStateBase<Enemy_Slime>(
                this,
                stateMachine,
                evasionDetail.animBoolName,
                battleState: () => battleState,
                enterSounds: ToSoundList(evasionDetail.enterSounds),
                exitSounds: ToSoundList(evasionDetail.exitSounds)
            );
        }
    }

    protected override void MapAbilityStates()
    {
        base.MapAbilityStates();

        if (abilityMap == null || abilityMap.Count == 0)
            return;

        foreach (var entry in abilityMap.Values)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.animBoolName))
                continue;

            switch (entry.action)
            {
                case BattleAction.Attack:
                    if (attackState != null && attackState.animBoolName == entry.animBoolName)
                        entry.state = attackState;
                    break;

                case BattleAction.Evade:
                    if (evasionState != null && evasionState.animBoolName == entry.animBoolName)
                        entry.state = evasionState;
                    break;

                case BattleAction.Jump:
                    break;

                case BattleAction.Stunned:
                    if (stunnedState != null && stunnedState.animBoolName == entry.animBoolName)
                    {
                        entry.state = stunnedState;
                        stunnedState.Configure(entry.stunDuration, entry.stunDirection);
                    }
                    break;

                case BattleAction.Teleport:
                    break;
            }
        }
    }

    public override void StartBattle()
    {
        base.StartBattle();
        cooldownSystem?.ApplyStartCooldowns();
    }

    private List<StateSound> ToSoundList(StateSound[] sounds)
    {
        if (sounds == null || sounds.Length == 0)
            return null;

        return new List<StateSound>(sounds);
    }

    private sealed class IdleWithTargets : IdleStateBase<Enemy_Slime>
    {
        private readonly System.Func<EnemyState> moveFactory;
        private readonly System.Func<EnemyState> battleFactory;

        public IdleWithTargets(
            Enemy_Slime enemy,
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

    public override void ChangeBattleCooldownRange(float minCooldown, float maxCooldown)
    {
        cooldownSystem?.ChangeBattleCooldownRange(minCooldown, maxCooldown);
    }

    public override void TempChangeBattleCooldownRange(float minCooldown, float maxCooldown, float duration)
    {
        cooldownSystem?.TempChangeBattleCooldownRange(minCooldown, maxCooldown, duration);
    }

    public override void PauseBattleCooldown(bool pause)
    {
        cooldownSystem?.PauseBattleCooldown(pause);
    }

    #endregion

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
            deadState = new DeadStateBase<Enemy_Slime>(this, stateMachine, "Idle");
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);

        if (slimeType == SlimeType.small)
            return;

        CreateSlimes(slimesToCreate, slimePrefab);
    }

    private void CreateSlimes(int _amountOfSlimes, GameObject _slimePrefab)
    {
        for (int i = 0; i < _amountOfSlimes; i++)
        {
            GameObject newSlime = Instantiate(_slimePrefab, transform.position, Quaternion.identity);

            newSlime.GetComponent<Enemy_Slime>().SetupSlime(facingDir);
        }
    }

    public void SetupSlime(int _facingDir)
    {
        if (_facingDir != facingDir)
            Flip();

        float xVelocity = Random.Range(minCreationVelocity.x, maxCreationVelocity.x);
        float yVelocity = Random.Range(minCreationVelocity.y, maxCreationVelocity.y);

        isKnocked = true;

        GetComponent<Rigidbody2D>().linearVelocity = new Vector2 (xVelocity * -facingDir, yVelocity);

        Invoke("CancelKnockback", 1.5f);
    }

    private void CancelKnockback() => isKnocked = false;
}
