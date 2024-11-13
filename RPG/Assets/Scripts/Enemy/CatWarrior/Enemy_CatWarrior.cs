using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_CatWarrior : Enemy_Boss
{
    #region States
    public CatWarrior_IdleState idleState { get; private set; }
    public CatWarrior_BattleState battleState { get; private set; }
    public CatWarrior_AttackState attackState { get; private set; }
    public CatWarrior_DeadState deadState { get; private set; }
    public CatWarrior_StartState startState { get; private set; }
    public CatWarrior_MoveState moveState { get; private set; }
    public CatWarrior_MagicState magicState { get; private set; }
    #endregion

    [Header("Tornado info")]
    [SerializeField] private GameObject tornadoPrefab;
    [SerializeField] private float tornadoSpeed = 15;
    [SerializeField] private float tornadoCooldown = 10;
    private float tornadoTimer;

    protected override void Awake()
    {
        base.Awake();

        idleState = new CatWarrior_IdleState(this, stateMachine, "Idle", this);
        battleState = new CatWarrior_BattleState(this, stateMachine, "Battle", this);
        attackState = new CatWarrior_AttackState(this, stateMachine, "Attack", this);
        deadState = new CatWarrior_DeadState(this, stateMachine, "Die", this);
        startState = new CatWarrior_StartState(this, stateMachine, "Idle", this);
        moveState = new CatWarrior_MoveState(this, stateMachine, "Move", this);
        magicState = new CatWarrior_MagicState(this, stateMachine, "Magic", this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(startState);
        tornadoTimer = 5f;
    }

    protected override void Update()
    {
        base.Update();
        tornadoTimer -= Time.deltaTime;
    }

    public override void Die()
    {
        base.Die();

        if (stats.isDeadZone)
        {
            deadState = new CatWarrior_DeadState(this, stateMachine, "Idle", this);
            anim.SetBool(lastAnimBoolName, true);
            anim.speed = 0;
            cd.enabled = false;
        }

        stateMachine.ChangeState(deadState);
    }

    public bool CanCastTornado()
    {
        if (tornadoTimer < 0)
        {
            tornadoTimer = tornadoCooldown;
            return true;
        }
        return false;
    }

    public override void AnimationSpecialAttackTrigger()
    {
        Vector3 offset = new Vector3(1 * facingDir, 0);
        GameObject castTornadoAttack = Instantiate(tornadoPrefab, transform.position + offset, Quaternion.identity);
        castTornadoAttack.GetComponent<Tornado_Controller>().SetupTornadoAttack(stats, tornadoSpeed * facingDir);
    }
}
