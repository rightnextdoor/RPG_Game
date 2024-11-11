using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_AirWalkState : EnemyState
{
    private Enemy_Wizard enemy;
    private float spellTimer;
    private float moveTimer;
    private int moveDir;
    public Wizard_AirWalkState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        spellTimer = 1.7f;
        moveTimer = 4.3f;
        enemy.rb.gravityScale = 0f;
        enemy.stats.MakeInvincible(true);

        int position = Random.Range(0, 10);

        if (position < 6)
        {
            enemy.moveToPosition = enemy.rightPosition.position;
            moveDir = -1;
        } else
        {
            enemy.moveToPosition = enemy.leftPosition.position;
            moveDir = 1;
        }     
    }

    public override void Exit()
    {
        base.Exit();
        enemy.lastTimeCastAirWalk = Time.time;
        enemy.stats.MakeInvincible(false);
    }

    public override void Update()
    {
        base.Update();

        spellTimer -= Time.deltaTime;
        moveTimer -= Time.deltaTime;

        if (spellTimer < 0f)
        {
            enemy.SetVelocity(enemy.moveSpeed * moveDir, rb.velocity.y);
            
        }

        if (moveTimer < 0f)
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(enemy.teleportState);
        } 
    }
}
