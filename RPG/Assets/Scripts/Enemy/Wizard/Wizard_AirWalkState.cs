using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wizard_AirWalkState : EnemyState
{
    private Enemy_Wizard enemy;
    private float spellTimer;
    public Wizard_AirWalkState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Wizard enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        spellTimer = 3f;
        Debug.Log("Enter aire walk state");
    }

    public override void Exit()
    {
        base.Exit();
        enemy.lastTimeCastAirWalk = Time.time;
    }

    public override void Update()
    {
        base.Update();

        spellTimer -= Time.deltaTime;

        if(spellTimer < 0) 
            stateMachine.ChangeState(enemy.teleportState);
    }
}
