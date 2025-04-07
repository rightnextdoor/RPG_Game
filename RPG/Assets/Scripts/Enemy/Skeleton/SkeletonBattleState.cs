using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonBattleState : EnemyState
{
    private Enemy_Skeleton enemy;
    private Transform player;

    public SkeletonBattleState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;

        if (player.GetComponent<PlayerStats>().isDead)
        {
            stateMachine.ChangeState(enemy.moveState);
            return;
        }

        stateTimer = enemy.battleTime;
        enemy.BattleStateFlipControll(player);
        enemy.chanceToMultiAttack += 5;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.anim.SetFloat("xVelocity", enemy.rb.velocity.x);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            if (enemy.IsPlayerDetected())
            {
                PlayerDetected();
            }
            else
            {
                stateMachine.ChangeState(enemy.moveState);
            }
            return;
        }

        PlayerDetected();
    }

    private void PlayerDetected()
    {
        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;

            if (enemy.IsPlayerDetected().distance <= enemy.meleeAttackDistance)
            {
                if (enemy.CanMultiAttack() && enemy.HasMultiAttack)
                {
                    stateMachine.ChangeState(enemy.attack2State);
                    AudioManager.instance.PlaySFX("SkeletonAttack", enemy.transform);
                    AudioManager.instance.PlaySFXWithDelay("SkeletonAttack", .5f);
                    if (enemy.IsSpearSkeleton)
                    {
                        AudioManager.instance.PlaySFXWithDelay("SkeletonAttack", .7f);
                    }
                }
                    
            }

            enemy.MeleeAttack(player, enemy.attackState, enemy.evasionState, "SkeletonAttack", false, 0);
        }
        else
        {

            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) > enemy.playerDistance)
                stateMachine.ChangeState(enemy.idleState);
        }

        enemy.BattleStateFlipControll(player);

    }

}
