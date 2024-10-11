using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OctopusWaterState : EnemyState
{
    private Enemy_Octopus enemy;
    private bool jumpInWater;
    private bool jumpOutWater;
    private Transform postion;
    private float fallTimer;

    private bool once;
    public OctopusWaterState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Octopus enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        once = true;
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void Update()
    {
        base.Update();

        PlayerIsCloseBy();

        fallTimer -= Time.deltaTime;
        if (fallTimer > 0 && jumpInWater)
            rb.velocity = new Vector2(0, 20);

        if (stateTimer < 0 && jumpInWater)
            MoveToStartPostion();

        if (stateTimer > 0 && jumpOutWater)
            rb.velocity = new Vector2(0, 30);

        if (stateTimer <= 0 && jumpOutWater)
        {
            enemy.cd.enabled = true;
            if (enemy.IsGroundDetected())
            {
                stateMachine.ChangeState(enemy.moveState);
                jumpOutWater = false;
                once = true;
            }
        }

    }

    private void MoveToStartPostion()
    {
        enemy.transform.position = enemy.startPostion;
        enemy.SetZeroVelocity();
        enemy.rb.gravityScale = 0;
        enemy.cd.enabled = false;
        jumpInWater = false;
        once = true;
    }

    private void PlayerIsCloseBy()
    {
        if (once)
        {
            if (enemy.IsPlayerInZone() == true && enemy.IsGroundDetected() == false && !jumpInWater)
                JumpOutWater();

            if (enemy.IsGroundDetected() == true && enemy.IsPlayerInZone() == false && !jumpOutWater)
            {
                JumpInWater();
            }
        }
    }

    private void JumpInWater()
    {
        enemy.cd.enabled = false;

        fallTimer = .2f;
        stateTimer = .9f;
        jumpInWater = true;
        once = false;
        AudioManager.instance.DelaySoundFX("OctopusJumpIn", null, stateTimer - .1f);
    }

    private void JumpOutWater()
    {
        enemy.anim.speed = 1;
        enemy.rb.gravityScale = 12;

        stateTimer = .4f;
        jumpOutWater = true;
        once = false;
        AudioManager.instance.PlaySFX("OctopusJumpOut", null);
    }
}
