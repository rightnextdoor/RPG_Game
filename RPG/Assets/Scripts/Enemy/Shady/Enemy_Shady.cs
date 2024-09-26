using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Enemy_Shady : Enemy
{
    [Header("Shady specific")]
    public float battleStateMoveSpeed;

    [SerializeField] private GameObject explosivePrefab;
    private GameObject currentBall;
    [SerializeField] private float growSpeed;
    [SerializeField] private float maxSize;

    [SerializeField] private float explosionSpeed = 7;
    [SerializeField] private float explosionTimer = 3;

    #region States
    public ShadyIdleState idleState { get; private set; }
    public ShadyMoveState moveState { get; private set; }
    public ShadyBattleState battleState { get; private set; }
    public ShadyStunnedState stunnedState { get; private set; }
    public ShadyDeadState deadState { get; private set; }
    public ShadyAttackState attackState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        idleState = new ShadyIdleState(this, stateMachine, "Idle", this);
        moveState = new ShadyMoveState(this, stateMachine, "Move", this);
        battleState = new ShadyBattleState(this, stateMachine, "Idle", this);
        stunnedState = new ShadyStunnedState(this, stateMachine, "Stunned", this);
        deadState = new ShadyDeadState(this, stateMachine, "Die", this);
        attackState = new ShadyAttackState(this, stateMachine, "Attack", this);
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
            deadState = new ShadyDeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }
        stateMachine.ChangeState(deadState);
    }

    public override void AnimationSpecialAttackTrigger()
    {
        CreateBall();
        currentBall.GetComponent<Explosive_Controller>().SetupExplosive(stats, growSpeed, maxSize, attackCheckRadius, explosionSpeed, explosionTimer);
    }

    private void CreateBall()
    {
        currentBall = Instantiate(explosivePrefab, attackCheck.position, Quaternion.identity);
        currentBall.transform.position = attackCheck.position;
    }

    public void SelfDestroy() => Destroy(gameObject);
}
