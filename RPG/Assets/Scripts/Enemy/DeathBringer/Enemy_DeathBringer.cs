using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Enemy_DeathBringer : Enemy_Boss
{
    [Header("Death Bringer info")]

    [Header("Spell cast details")]
    [SerializeField] private GameObject spellPrefab;
    public int amountOfSpells;
    public float spellCooldown;

    public float lastTimeCast;
    [SerializeField] private float spellStateCooldown;
    [SerializeField] private Vector2 spellOffset;

    #region States
    public DeathBringerIdleState idleState { get; private set; }
    public DeathBringerBattleState battleState { get; private set; }
    public DeathBringerAttackState attackState { get; private set; }
    public DeathBringerDeadState deadState { get; private set; }
    public DeathBringerTeleportState teleportState { get; private set; }
    public DeathBringerSpellCastState spellCastState { get; private set; }
    public DeathBringerStartState startState { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        SetupDefaultFacingDir(-1);

        idleState = new DeathBringerIdleState(this, stateMachine, "Idle", this);
        battleState = new DeathBringerBattleState(this, stateMachine, "Battle", this);
        attackState = new DeathBringerAttackState(this, stateMachine, "Attack", this);
        deadState = new DeathBringerDeadState(this, stateMachine, "Die", this);
        teleportState = new DeathBringerTeleportState(this, stateMachine, "Teleport", this);
        spellCastState = new DeathBringerSpellCastState(this, stateMachine, "SpellCast", this);
        startState = new DeathBringerStartState(this, stateMachine, "Idle", this);
    }
    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new DeathBringerDeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void UnlockEquipment()
    {
        UnlockManager.instance.BossUnlockData(BossType.DeathBringer);
    }

    public override bool CanTeleport()
    {
        if (stage == Stage.Stage_1)
            return false;

        return base.CanTeleport();
    }

    public void CastSpell()
    {
        Player player = PlayerManager.instance.player;

        float xOffset = 0;

        if (player.rb.velocity.x != 0)
            xOffset = player.facingDir * spellOffset.x;

        Vector3 spellPosition = new Vector3(player.transform.position.x + xOffset, player.transform.position.y + spellOffset.y);

        GameObject newSpell = Instantiate(spellPrefab, spellPosition, Quaternion.identity);
        newSpell.GetComponent<DeathBringerSpell_Controller>().SetupSpell(stats);
    }

    public override void FindPosition()
    {
        base.FindPosition();
        AudioManager.instance.PlaySFX("DeathBringerTeleport2", null);
    }
  

    public bool CanDoSpellCast()
    {
        if (Time.time >= lastTimeCast + spellStateCooldown)
        {
            return true;
        }
        return false;
    }

    public override void StartNextStage()
    {
        base.StartNextStage();

        switch (stage)
        {
            case Stage.Stage_2:
                stats.IncreaseStats(10, stats.strength);
                stats.IncreaseStats(5, stats.armor);
                moveSpeed += 1;
                break;
            case Stage.Stage_3:
                stats.IncreaseStats(20, stats.strength);
                moveSpeed += 3;
                break;
        }
    }

}
