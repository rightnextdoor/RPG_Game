using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Enemy_DragonWarrior : Enemy_Boss
{
    #region States
    public DragonWarrior_IdleState idleState { get; private set; }
    public DragonWarrior_BattleState battleState { get; private set; }
    public DragonWarrior_AttackState attackState { get; private set; }
    public DragonWarrior_DeadState deadState { get; private set; }
    public DragonWarrior_StartState startState { get; private set; }
    #endregion

    [Header("Attack info")]
    [SerializeField] private GameObject fireAttackPrefab;
    [SerializeField] private float fireAttackSpeed = 6f;
    [SerializeField] private float fireAttackExplosionTimer = 3f;

    protected override void Awake()
    {
        base.Awake();

        idleState = new DragonWarrior_IdleState(this, stateMachine, "Idle", this);
        battleState = new DragonWarrior_BattleState(this, stateMachine, "Battle", this);
        attackState = new DragonWarrior_AttackState(this, stateMachine, "Attack", this);
        deadState = new DragonWarrior_DeadState(this, stateMachine, "Die", this);
        startState = new DragonWarrior_StartState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new DragonWarrior_DeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void UnlockEquipment()
    {
        UnlockManager.instance.BossUnlockData(BossType.DragonWarrior);
    }

    public override void SelfDestroy() => Destroy(transform.parent.gameObject, .5f);

    public override void AttackTrigger(string attackName, string checkLabel, string spawnName = null)
    {
        GameObject castFireBall = Instantiate(fireAttackPrefab, attackCheck.position, Quaternion.identity);
        castFireBall.GetComponent<DragonWarrior_FireBlast_Controller>().SetupFireBlast(fireAttackSpeed * facingDir, stats, attackCheckRadius, fireAttackExplosionTimer);
        //AudioManager.instance.PlaySFX("Wizard_FireBallAttack", null);
    }
}
