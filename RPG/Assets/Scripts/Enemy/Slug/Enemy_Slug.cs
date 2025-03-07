using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Slug : Enemy_Regular
{
    [Header("Slug info")]
    public Transform[] waypoints;
    [HideInInspector]public Transform target;
    public bool canFlip;

    #region States
    public SlugMoveState moveState { get; private set; }
    public SlugDeadState deadState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        moveState = new SlugMoveState(this, stateMachine, "Move", this);
        deadState = new SlugDeadState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(moveState);

        target = waypoints[0];
    }

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
        Destroy(transform.parent.gameObject, 2f);
    }

    protected override void CheckIfEnemyisDead()
    {
        if (enemyData.isDead)
        {
            Destroy(transform.parent.gameObject);
        }
    }
    public override void SelfDestroy() => Destroy(transform.parent.gameObject, 2f);
}
