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
    [SerializeField] private Vector2 playerJumpPadding = new Vector2(1.15f, 1.35f);

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

        CreateSlimes();
    }

    #region Create Slimes
    private void CreateSlimes()
    {
        int slimeCount = Mathf.Max(1, Random.Range(1, Mathf.Max(1, slimesToCreate) + 1));
        List<Vector2> landingSpots = new List<Vector2>();
        float deadSlimeHeight = GetSlimeHeight();

        for (int i = 0; i < slimeCount; i++)
            landingSpots.Add(GetSplitLandingSpot(landingSpots));

        for (int i = 0; i < slimeCount; i++)
        {
            GameObject newSlime = Instantiate(slimePrefab, transform.position, Quaternion.identity);
            Enemy_Slime slime = newSlime.GetComponent<Enemy_Slime>();

            if (slime != null)
                slime.SetupSlime(landingSpots[i], deadSlimeHeight);
        }
    }

    private void SetupSlime(Vector2 landingSpot, float deadSlimeHeight)
    {
        FacePlayer();
        JumpToLandingSpot(landingSpot, deadSlimeHeight);
    }

    private void JumpToLandingSpot(Vector2 landingSpot, float deadSlimeHeight)
    {
        Rigidbody2D slimeRb = GetComponent<Rigidbody2D>();
        if (slimeRb == null)
            return;

        float gravityScale = slimeRb.gravityScale;
        if (gravityScale <= 0f)
            gravityScale = 1f;

        float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        if (gravity <= 0f)
            gravity = 9.81f;

        Transform player = PlayerManager.instance?.player?.transform;
        float playerHeight = deadSlimeHeight;

        if (player != null)
        {
            CapsuleCollider2D playerCollider = player.GetComponent<CapsuleCollider2D>();
            if (playerCollider != null)
                playerHeight = playerCollider.bounds.size.y;
            else
            {
                SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();
                if (playerSprite != null)
                    playerHeight = playerSprite.bounds.size.y;
            }
        }

        Vector2 startPos = slimeRb.position;
        float distanceX = landingSpot.x - startPos.x;

        float extraHeight = Random.Range(playerJumpPadding.x, playerJumpPadding.y);
        float jumpHeight = playerHeight + extraHeight;

        float yVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
        float airTime = (2f * yVelocity) / gravity;
        float xVelocity = Mathf.Abs(distanceX) <= 0.01f ? 0f : distanceX / airTime;

        isKnocked = true;
        slimeRb.linearVelocity = Vector2.zero;
        slimeRb.linearVelocity = new Vector2(xVelocity, yVelocity);

        StartCoroutine(WaitForSplitLanding(slimeRb));
    }

    private IEnumerator WaitForSplitLanding(Rigidbody2D slimeRb)
    {
        while (slimeRb != null && slimeRb.linearVelocity.y > 0.01f)
            yield return null;

        while (slimeRb != null && !IsGroundDetected())
            yield return null;

        isKnocked = false;
    }

    #region Helpers
    private void FacePlayer()
    {
        Transform player = PlayerManager.instance?.player?.transform;
        if (player == null)
            return;

        if (player.position.x > transform.position.x && facingDir == -1)
            Flip();
        else if (player.position.x < transform.position.x && facingDir == 1)
            Flip();
    }

    private Vector2 GetSplitLandingSpot(List<Vector2> usedSpots)
    {
        float deadWidth = GetSlimeWidth();
        float splitWidth = deadWidth * Random.Range(1.7f, 1.9f);
        float halfWidth = splitWidth * 0.5f;

        float childWidth = GetChildSlimeWidth();
        float minSpacing = childWidth * 0.75f;

        Vector2 center = transform.position;
        Vector2 bestSpot = center;
        float bestDistance = -1f;

        for (int i = 0; i < 10; i++)
        {
            float xOffset = Random.Range(-halfWidth, halfWidth);
            Vector2 candidate = new Vector2(center.x + xOffset, center.y);
            float closestDistance = GetClosestLandingDistance(candidate, usedSpots);

            if (closestDistance >= minSpacing)
                return candidate;

            if (closestDistance > bestDistance)
            {
                bestDistance = closestDistance;
                bestSpot = candidate;
            }
        }

        return bestSpot;
    }

    private float GetClosestLandingDistance(Vector2 spot, List<Vector2> usedSpots)
    {
        if (usedSpots == null || usedSpots.Count == 0)
            return float.MaxValue;

        float closest = float.MaxValue;

        foreach (Vector2 used in usedSpots)
        {
            float distance = Mathf.Abs(spot.x - used.x);
            if (distance < closest)
                closest = distance;
        }

        return closest;
    }

    private float GetSlimeWidth()
    {
        CapsuleCollider2D slimeCollider = cd != null ? cd : GetComponent<CapsuleCollider2D>();
        if (slimeCollider != null)
            return slimeCollider.bounds.size.x;

        SpriteRenderer slimeSprite = sr != null ? sr : GetComponentInChildren<SpriteRenderer>();
        if (slimeSprite != null)
            return slimeSprite.bounds.size.x;

        return transform.lossyScale.x;
    }

    private float GetSlimeHeight()
    {
        CapsuleCollider2D slimeCollider = cd != null ? cd : GetComponent<CapsuleCollider2D>();
        if (slimeCollider != null)
            return slimeCollider.bounds.size.y;

        SpriteRenderer slimeSprite = sr != null ? sr : GetComponentInChildren<SpriteRenderer>();
        if (slimeSprite != null)
            return slimeSprite.bounds.size.y;

        return transform.lossyScale.y;
    }

    private float GetChildSlimeWidth()
    {
        if (slimePrefab == null)
            return 1f;

        CapsuleCollider2D childCollider = slimePrefab.GetComponent<CapsuleCollider2D>();
        if (childCollider != null)
            return childCollider.size.x * Mathf.Abs(slimePrefab.transform.lossyScale.x);

        SpriteRenderer childSprite = slimePrefab.GetComponentInChildren<SpriteRenderer>();
        if (childSprite != null)
            return childSprite.bounds.size.x;

        return slimePrefab.transform.lossyScale.x;
    }

    #endregion

    #endregion
}
