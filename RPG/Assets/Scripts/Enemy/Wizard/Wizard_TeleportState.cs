using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;

public class Wizard_TeleportState : EnemyState
{
    private Enemy_Wizard enemy;
    public Wizard_TeleportState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        //AudioManager.instance.PlaySFX("DeathBringerLaugh", null);
        //AudioManager.instance.PlaySFX("DeathBringerTeleport", null);
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

        if (triggerCalled)
        {
            stateMachine.ChangeState(enemy.spellState);
        }
    }
}
