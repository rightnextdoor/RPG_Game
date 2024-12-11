using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Regular : Enemy
{
    [Header("Regular enemy info")]
    public EnemyData_Regular enemyData;
    [Space]
    [Header("Stunned info")]
    public float stunDuration = 1;
    public Vector2 stunDirection = new Vector2(10, 12);
    protected bool canBeStunned;
    [SerializeField] protected GameObject counterImage;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        StartCoroutine(CheckWithDelay());
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void Die()
    {
        base.Die();

        enemyData.isDead = true;

        Destroy(gameObject, 2f);
    }

    private IEnumerator CheckWithDelay()
    {
        yield return new WaitForSeconds(.1f);

        CheckIfEnemyisDead();
    }

    protected virtual void CheckIfEnemyisDead()
    {
        if (enemyData.isDead)
        {
            Destroy(gameObject);
        }
    }

    #region Counter Attack Window
    public override void OpenCounterAttackWindow()
    {
        canBeStunned = true;
        counterImage.SetActive(true);
    }

    public override void CloseCounterAttackWindow()
    {
        canBeStunned = false;
        counterImage.SetActive(false);
    }
    #endregion

    public override bool CanBeStunned()
    {
        if (canBeStunned)
        {
            CloseCounterAttackWindow();
            return true;
        }
        return false;
    }

    public void SummonEnemy(bool _isSummon)
    {
        isSummon = _isSummon;
        enemyData.canBeSummon = _isSummon;
    }

}
