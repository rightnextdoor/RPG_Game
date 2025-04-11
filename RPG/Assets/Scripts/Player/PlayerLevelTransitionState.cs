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

        // Apply movement direction once at entry
        if (player.transtionUp)
            player.SetVelocity(0, player.jumpForce);
        else if (player.transtionDown)
            player.SetVelocity(0, -player.jumpForce);
        else
            player.SetVelocity(player.moveSpeed * player.facingDir, 0);

        // Wait a short time before checking to exit state (optional)
        if (!PlayerManager.instance.IsLevelTranstion())
        {
            player.SetZeroVelocity(); // stop motion after cutscene
            stateMachine.ChangeState(player.idleState);
        }
    }

}
