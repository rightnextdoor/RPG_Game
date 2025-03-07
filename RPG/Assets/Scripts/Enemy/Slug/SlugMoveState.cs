using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SlugMoveState : EnemyState
{
    private Enemy_Slug enemy;
    private int wavePointIndex = 0;
    
    public SlugMoveState(Enemy_Regular _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Slug enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.moveTime;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0f)
            enemy.RunIntoPlayerAttack(enemy.moveState);

        Vector3 dir = enemy.target.position - enemy.transform.position;
        enemy.transform.Translate(dir.normalized * enemy.moveSpeed * Time.deltaTime, Space.World);

        if (Vector3.Distance(enemy.transform.position, enemy.target.position) <= 0.48f)
            {      
            GetNextWaypoint();
            if(enemy.canFlip)
                enemy.Flip();
        }

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
}
