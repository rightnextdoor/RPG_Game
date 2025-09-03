using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] public bool allowBattleInterruptFromMove = true;

    [Header("Patrol / Traverse area")]
    [Space(6)]
    [SerializeField] public Transform patrolAreaAnchor;           
    [SerializeField] public Vector2 patrolAreaSize = new Vector2(8f, 2f);
    [SerializeField] public bool enterIdleOnPatrolBoundary = true; 

    [Header("Random stop behavior (Patrol mode only)")]
    [SerializeField] public bool moveStopsEnabled = true;
    [SerializeField] public Vector2 moveStopIntervalRange = new Vector2(2f, 5f);   
    [SerializeField] public Vector2 moveStopDurationRange = new Vector2(0.4f, 1f); 
    [SerializeField][Range(0f, 1f)] public float flipOnStopChance = 0.5f;

    [Header("Surface crawler")]
    [Space(6)]
    [SerializeField] public LayerMask surfaceCrawlMask = ~0;
    [SerializeField][Min(0.01f)] public float surfaceProbeAhead = 0.4f;
    [SerializeField][Min(0.01f)] public float surfaceProbeDown = 0.5f;

    #endregion

    [Space(2)]
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
        if(enemyData != null ) 
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
        if(enemyData != null)
            enemyData.canBeSummon = _isSummon;
    }

}
