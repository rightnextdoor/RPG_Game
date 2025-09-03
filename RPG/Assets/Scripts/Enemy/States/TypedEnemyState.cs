using System;

public abstract class TypedEnemyState<TEnemy> : EnemyState where TEnemy : Enemy
{
    protected readonly TEnemy enemy;

    protected TypedEnemyState(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as TEnemy
            ?? throw new InvalidCastException($"{GetType().Name} expected {typeof(TEnemy).Name} but got {enemyBase.GetType().Name}");
    }
}
