using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_KnightLaughState : EnemyState
{
    private Enemy_Black_Knight enemy;
    private float laughTimer;
    public Black_KnightLaughState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Black_Knight enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        laughTimer = 1f;
        AudioManager.instance.PlaySFX("BlackKnightLaugh");
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        laughTimer -= Time.deltaTime;
        enemy.SetZeroVelocity();
        if (laughTimer < 0)
        {
            if (enemy.CanSummonSkeleton())
            {
                stateMachine.ChangeState(enemy.summonState);
            }
            else
                stateMachine.ChangeState(enemy.battleState);
        }

    }
}
