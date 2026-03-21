using System.Collections.Generic;
using UnityEngine;

public class AttackStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy
{
    protected AttackDetail detail;
    protected System.Func<EnemyState> nextStateFactory;

    protected int remainingAttacks;
    protected bool isMulti;

    protected bool isLingering;

    public AttackStateBase(
    TEnemy enemyBase,
    EnemyStateMachine stateMachine,
    string animBoolName,
    System.Func<EnemyState> nextStateFactory,
    List<StateSound> enterSounds = null,
    List<StateSound> exitSounds = null
    ) : base(enemyBase, stateMachine, animBoolName)
    {
        this.nextStateFactory = nextStateFactory;
    }

    public void Configure(AttackDetail attackDetail, System.Func<EnemyState> nextFactory = null)
    {
        detail = attackDetail;

        if (nextFactory != null)
            nextStateFactory = nextFactory;

        int amount = Mathf.Max(1, attackDetail.attackAmount);
        remainingAttacks = amount;
        isMulti = amount > 1;
        isLingering = false;
    }

    public override void Enter()
    {
        base.Enter();
        enemy.SetCurrentAttackDetail(detail);
        stateTimer = 0f;
        isLingering = false;
    }

    public override void Update()
    {
        base.Update();

        enemy.SetZeroVelocity();

        if (isLingering)
        {
            if (stateTimer <= 0f && nextStateFactory != null)
                stateMachine.ChangeState(nextStateFactory());
            return;
        }

        if (isMulti && CanAttack())
            MultiAttack();
        else
            SingleAttack();
    }

    public override void Exit()
    {
        base.Exit();
        enemy.ClearCurrentAttackDetail();
    }

    protected virtual void SingleAttack()
    {
        if (triggerCalled)
            Linger();
    }

    protected virtual void MultiAttack()
    {
        if (triggerCalled)
        { 
            if (remainingAttacks > 0)
            {       
                AttackTrigger(detail.animBoolName);   
            }
            else
            {
                Linger();
            }
        }
    }

    private bool CanAttack()
    {
        if (remainingAttacks > 0 && stateTimer < 0)
        {
            remainingAttacks--;
            float min = detail.attackTimeMin;
            float max = detail.attackTimeMax;
            if (max < min) max = min;
            stateTimer = (min == 0f && max == 0f) ? 0f : Random.Range(min, max);
            return true;
        }

        return false;
    }

    protected virtual void Linger()
    {
        isLingering = true;
        stateTimer = Mathf.Max(0f, detail.lingerTime);
    }
}
