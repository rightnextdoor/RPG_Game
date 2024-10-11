using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctopusDeadState : EnemyState
{
    private Enemy_Octopus enemy;
    public OctopusDeadState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Octopus enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        AudioManager.instance.PlaySFX("OctopusDie", enemy.transform);
        enemy.anim.SetBool(enemy.lastAnimBoolName, true);
        enemy.anim.speed = 0;
        enemy.cd.enabled = false;

        stateTimer = .15f;
        enemy.SelfDestroy();
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer > 0)
            rb.velocity = new Vector2(0, 10);
    }
}
