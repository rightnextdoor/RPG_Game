using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SlugMoveState : EnemyState
{
    private Enemy_Slug enemy;
    private float hitTimer = 0;
    private int wavePointIndex = 0;
    private float dist;
    
    public SlugMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Slug enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.moveTime;
        enemy.target = enemy.waypoints[0];
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        hitTimer -= Time.deltaTime;
        Vector3 dir = enemy.target.position - enemy.transform.position;
        enemy.transform.Translate(dir.normalized * enemy.moveSpeed * Time.deltaTime, Space.World);
        dist = Vector2.Distance(enemy.transform.position, enemy.target.transform.position);
        //enemy.transform.position = Vector2.MoveTowards(enemy.transform.position, enemy.target.transform.position,
        //    enemy.moveSpeed * Time.deltaTime);

        //if (dist < 0.48f)
        if (Vector3.Distance(enemy.transform.position, enemy.target.position) <= 0.48f)
            {      
            GetNextWaypoint();
            if(enemy.canFlip)
                enemy.Flip();
        }

        Attack();

    }

    private void GetNextWaypoint()
    {
        if(!enemy.canFlip)
            EnemyRotation();

        if (wavePointIndex >= enemy.waypoints.Length - 1)
        {
            wavePointIndex = 0;
        }
        else
        {
            wavePointIndex++;
        }

        enemy.target = enemy.waypoints[wavePointIndex];
    }

    private void EnemyRotation()
    {
        Vector3 currRot = enemy.transform.eulerAngles;
        currRot.z += enemy.waypoints[wavePointIndex].transform.eulerAngles.z;
        enemy.transform.eulerAngles = currRot;
    }

    private void Attack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheck.position, enemy.attackCheckRadius);

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                PlayerStats target = hit.GetComponent<PlayerStats>();
                if (hitTimer < 0f)
                {
                    enemy.stats.DoDamage(target);
                    //ToDo: knock the player back when hit
                    hitTimer = enemy.hitTimer;
                }
                
            }
        }
    }
}
