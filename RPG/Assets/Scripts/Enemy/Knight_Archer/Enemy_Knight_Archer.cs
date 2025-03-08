using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Knight_Archer : Enemy_Regular
{
    [Header("Archer specific info")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed;

    #region States
    public Knight_Archer_IdleState idleState { get; private set; }
    public Knight_Archer_MoveState moveState { get; private set; }
    public Knight_Archer_BattleState battleState { get; private set; }
    public Knight_Archer_AttackState attackState { get; private set; }
    public Knight_Archer_AttackState attack2State { get; private set; }
    public Knight_Archer_StunnedState stunnedState { get; private set; }
    public Knight_Archer_DeadState deadState { get; private set; }
    public Knight_Archer_EvasionState evasionState { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new Knight_Archer_IdleState(this, stateMachine, "Idle", this);
        moveState = new Knight_Archer_MoveState(this, stateMachine, "Move", this);
        battleState = new Knight_Archer_BattleState(this, stateMachine, "Battle", this);
        attackState = new Knight_Archer_AttackState(this, stateMachine, "Attack", this);
        attack2State = new Knight_Archer_AttackState(this, stateMachine, "Attack2", this);
        stunnedState = new Knight_Archer_StunnedState(this, stateMachine, "Stunned", this);
        deadState = new Knight_Archer_DeadState(this, stateMachine, "Die", this);
        evasionState = new Knight_Archer_EvasionState(this, stateMachine, "Move", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
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
            deadState = new Knight_Archer_DeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public override void AnimationSpecialAttackTrigger()
    {
        GameObject newArrow = Instantiate(arrowPrefab, attackCheck.position, Quaternion.identity);
        Transform player = PlayerManager.instance.player.transform;
        newArrow.GetComponent<Arrow_Controller>().SetupArrow(arrowSpeed * facingDir, stats, player.position);
        AudioManager.instance.PlaySFX("ArcherAttack", transform);
    }
}
