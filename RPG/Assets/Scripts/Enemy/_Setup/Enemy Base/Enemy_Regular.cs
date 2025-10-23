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

    [Header("Patrol / Traverse area")]
    [Space(6)]
    [SerializeField] public Transform patrolAreaAnchor;
    [System.NonSerialized] private Vector3 patrolAnchorFrozenWorld;
    [SerializeField] public Vector2 patrolAreaSize = new Vector2(8f, 2f);
    [System.NonSerialized] public Vector2 patrolCenter;
    [System.NonSerialized] public float patrolLeftX, patrolRightX;
    [HideInInspector] public bool patrolSnapshotDone;

    [Header("Traverse Stops")]
    [SerializeField] public float traverseStopMinSeconds = 1.0f;
    [SerializeField] public float traverseStopMaxSeconds = 2.5f;
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

        SnapshotPatrolArea();

        StartCoroutine(CheckWithDelay());
    }

    protected override void Update()
    {
        base.Update();

        if (patrolSnapshotDone && patrolAreaAnchor != null)
            patrolAreaAnchor.position = patrolAnchorFrozenWorld;
    }

    public override void Die()
    {
        base.Die();
        if (enemyData != null)
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
        if (enemyData != null)
            enemyData.canBeSummon = _isSummon;
    }

    private void SnapshotPatrolArea()
    {
        Vector2 center = patrolAreaAnchor ? (Vector2)patrolAreaAnchor.position
                                          : (Vector2)transform.position;

        patrolCenter = center;
        float half = Mathf.Max(0.25f, patrolAreaSize.x * 0.5f);
        patrolLeftX = center.x - half;
        patrolRightX = center.x + half;

        patrolAnchorFrozenWorld = patrolAreaAnchor ? patrolAreaAnchor.position
                                               : new Vector3(center.x, center.y, 0f);

        patrolSnapshotDone = true;
    }

    public bool IsOutsidePatrol(float x) => x < patrolLeftX || x > patrolRightX;

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

#if UNITY_EDITOR
        Vector3 center = patrolSnapshotDone
            ? new Vector3(patrolCenter.x, patrolCenter.y, 0f)
            : (patrolAreaAnchor ? patrolAreaAnchor.position : transform.position);

        float width = Mathf.Max(0.01f, patrolAreaSize.x);
        float height = Mathf.Max(0.10f, patrolAreaSize.y);
        Vector3 box = new Vector3(width, height, 0.1f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawCube(center, box);

        Gizmos.color = new Color(0f, 0.8f, 0f, 1f);
        Gizmos.DrawWireCube(center, box);

        float leftX = patrolSnapshotDone ? patrolLeftX : center.x - width * 0.5f;
        float rightX = patrolSnapshotDone ? patrolRightX : center.x + width * 0.5f;
        float tickH = height * 0.5f;
        Vector3 up = Vector3.up * tickH;

        Gizmos.DrawLine(new Vector3(leftX, center.y, 0f) - up, new Vector3(leftX, center.y, 0f) + up);
        Gizmos.DrawLine(new Vector3(rightX, center.y, 0f) - up, new Vector3(rightX, center.y, 0f) + up);
#endif
    }


}
