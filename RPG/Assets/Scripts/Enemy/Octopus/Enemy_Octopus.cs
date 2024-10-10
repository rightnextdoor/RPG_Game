using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy_Octopus : Enemy
{
    [Header("Octopus info")]
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private float bubbleSpeed = 7;
    [SerializeField] private float explosionTimer = 3;
    [SerializeField] private BoxCollider2D playerDetectedZone;
    public Vector3 startPostion;

    #region States
    public OctopusMoveState moveState { get; private set; }
    public OctopusBattleState battleState { get; private set; }
    public OctopusAttackState attackState { get; private set; }
    public OctopusDeadState deadState { get; private set; }
    public OctopusWaterState waterState { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        battleState = new OctopusBattleState(this, stateMachine, "Move", this);
        attackState = new OctopusAttackState(this, stateMachine, "Attack", this);
        moveState = new OctopusMoveState(this, stateMachine, "Move", this);
        deadState = new OctopusDeadState(this, stateMachine, "Idle", this);
        waterState = new OctopusWaterState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();

        startPostion = transform.position;
        stateMachine.Initialize(waterState);
    }

    protected override void Update()
    {
        base.Update();

    }

    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }
    public override void AnimationSpecialAttackTrigger()
    {
        GameObject newBubble = Instantiate(bubblePrefab, attackCheck.position, Quaternion.identity);
        newBubble.GetComponent<Bubble_Controller>().SetupBubble(bubbleSpeed * facingDir, stats, attackCheckRadius, explosionTimer);
    }

    public bool IsPlayerInZone() => playerDetectedZone.GetComponent<EnemyZone>().PlayerInZone;
}
