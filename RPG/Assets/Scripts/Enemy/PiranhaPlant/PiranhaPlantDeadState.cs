using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiranhaPlantDeadState : EnemyState
{
    private Enemy_PiranhaPlant enemy;
    public PiranhaPlantDeadState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_PiranhaPlant enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("PiranhaPlantDie", enemy.transform);
        enemy.anim.SetBool(enemy.lastAnimBoolName, true);
        enemy.anim.speed = 0;
        enemy.cd.enabled = false;

        stateTimer = .15f;
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer > 0)
            rb.velocity = new Vector2(0, 10);
    }
}
