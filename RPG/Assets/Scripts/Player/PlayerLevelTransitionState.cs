using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLevelTransitionState : PlayerState
{
    private float gravityScale;

    public PlayerLevelTransitionState(Player _player, PlayerStateMachine _stateMachine, string animBoolName) : base(_player, _stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.DisableControl();
        gravityScale = player.rb.gravityScale;
        player.rb.gravityScale = 0;
        player.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();
        player.rb.gravityScale = gravityScale;
        player.EnableControl();
    }

    public override void Update()
    {
        base.Update();

        if (player.transtionUp)
        {
            player.SetVelocity(rb.velocity.x, player.jumpForce);
        } else if (player.transtionDown)
        {
            player.SetVelocity(rb.velocity.x, -player.jumpForce);
        } else
        {
            player.SetVelocity(player.moveSpeed * player.facingDir, rb.velocity.y);
        }

        if(!PlayerManager.instance.IsLevelTranstion())
            stateMachine.ChangeState(player.idleState);

    }
}
