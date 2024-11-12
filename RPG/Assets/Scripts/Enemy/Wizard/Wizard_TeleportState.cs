using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;

public class Wizard_TeleportState : EnemyState
{
    private Enemy_Wizard enemy;
    private float teleportTimer;
    public Wizard_TeleportState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        teleportTimer = 1.7f;
        AudioManager.instance.PlaySFX("Wizard_TeleportIn", null);
        enemy.stats.MakeInvincible(true);
        
    }

    public override void Exit()
    {
        base.Exit();
        
        enemy.stats.MakeInvincible(false);
    }

    public override void Update()
    {
        base.Update();

        teleportTimer -= Time.deltaTime;
        if (teleportTimer < 0)
        {
            enemy.rb.gravityScale = enemy.defaultGravity;
        }

        
        if (triggerCalled)
        {            
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}
