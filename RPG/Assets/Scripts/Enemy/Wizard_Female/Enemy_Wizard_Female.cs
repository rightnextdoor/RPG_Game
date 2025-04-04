using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Wizard_Female : Enemy_Regular
{
    [Header("Projectile specific info")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileExplosionTimer = 3f;

    #region States
    public Wizard_Female_IdleState idleState { get; private set; }
    public Wizard_Female_MoveState moveState { get; private set; }
    public Wizard_Female_BattleState battleState { get; private set; }
    public Wizard_Female_AttackState attackState { get; private set; }
    public Wizard_Female_DeadState deadState { get; private set; }
    public Wizard_Female_EvasionState evasionState { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new Wizard_Female_IdleState(this, stateMachine, "Idle", this);
        moveState = new Wizard_Female_MoveState(this, stateMachine, "Move", this);
        battleState = new Wizard_Female_BattleState(this, stateMachine, "Battle", this);
        attackState = new Wizard_Female_AttackState(this, stateMachine, "Attack", this);;
        deadState = new Wizard_Female_DeadState(this, stateMachine, "Die", this);
        evasionState = new Wizard_Female_EvasionState(this, stateMachine, "Move", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    public override void Die()
    {
        base.Die();
        if (stats.isDeadZone)
        {
            deadState = new Wizard_Female_DeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void AnimationSpecialAttackTrigger()
    {
        Transform player = PlayerManager.instance.player.transform;
        Vector3 direction = player.position - wallCheck.position;

        if (direction.x < 1 && direction.x > -1)
        {
            stateMachine.ChangeState(evasionState);
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, attackCheck.position, Quaternion.identity);
        projectile.GetComponent<WizardProjectile_Controller>().SetupProjectile(projectileSpeed * facingDir, stats, attackCheckRadius, projectileExplosionTimer, player.position, direction);
        //AudioManager.instance.PlaySFX("ArcherAttack", transform);
    }
}
