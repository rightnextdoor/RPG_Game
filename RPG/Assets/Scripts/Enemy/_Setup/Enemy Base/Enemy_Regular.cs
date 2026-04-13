using System.Collections;
using UnityEngine;

public class Enemy_Regular : Enemy
{
    [Header("Regular enemy info")]
    [Space(2)]
    public EnemyData_Regular enemyData;

    #region Move State
    public enum RegularMoveMode { PatrolArea, TraverseNoStops, SurfaceCrawler }
    [Header("Move State")]
    [Space(2)]
    [SerializeField] public RegularMoveMode moveMode = RegularMoveMode.PatrolArea;

    [Header("General")]
    [SerializeField] public float moveSpeedMultiplier = 1f;
    [SerializeField] public bool randomizeFacingOnMoveEnter = true;

    [Header("Traverse Stops")]
    [SerializeField] public float traverseStopMinSeconds = 1.0f;
    [SerializeField] public float traverseStopMaxSeconds = 2.5f;

    #endregion

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
        if (enemyData != null)
            enemyData.isDead = true;

    }

    private IEnumerator CheckWithDelay()
    {
        yield return new WaitForSeconds(.1f);

        CheckIfEnemyisDead();
    }

    protected virtual void CheckIfEnemyisDead()
    {
        if (enemyData == null)
            return;

        if (enemyData.isDead)
        {
            Destroy(gameObject);
        }
    }

    #region Counter Attack Window
    public override void OpenCounterAttackWindow()
    {
        var stunnedDetail = GetStunnedDetail();
        if (stunnedDetail == null)
            return;

        stunnedDetail.canBeStunned = true;

        if (stunnedDetail.counterImage != null)
            stunnedDetail.counterImage.SetActive(true);
    }

    public override void CloseCounterAttackWindow()
    {
        var stunnedDetail = GetStunnedDetail();
        if (stunnedDetail == null)
            return;

        stunnedDetail.canBeStunned = false;

        if (stunnedDetail.counterImage != null)
            stunnedDetail.counterImage.SetActive(false);
    }
    public override bool CanBeStunned()
    {
        var stunnedDetail = GetStunnedDetail();
        if (stunnedDetail == null)
            return false;

        if (stunnedDetail.canBeStunned)
        {
            CloseCounterAttackWindow();
            return true;
        }

        return false;
    }
    private AttackDetail GetStunnedDetail()
    {
        if (attackDetails == null || attackDetails.Count == 0)
            return null;

        for (int i = 0; i < attackDetails.Count; i++)
        {
            var detail = attackDetails[i];
            if (detail != null && detail.action == BattleAction.Stunned)
                return detail;
        }

        return null;
    }
    #endregion

    public void SummonEnemy(bool _isSummon)
    {
        isSummon = _isSummon;
        if (enemyData != null)
            enemyData.canBeSummon = _isSummon;
    }

}
