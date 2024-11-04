using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_FireBallState : EnemyState
{
    private Enemy_Wizard enemy;
    private float spellTimer;
    public Wizard_FireBallState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        spellTimer = 1.5f;
        Debug.Log("Enter fire ball state");
    }

    public override void Exit()
    {
        base.Exit();
        enemy.lastTimeCastFireBall = Time.time;
    }

    public override void Update()
    {
        base.Update();

        spellTimer -= Time.deltaTime;

        if (spellTimer < 0)
        {
            if (enemy.CanTeleport())
            {
                stateMachine.ChangeState(enemy.teleportState);
            }
            else
            {
                stateMachine.ChangeState(enemy.battleState);
            }
        }

    }
}
