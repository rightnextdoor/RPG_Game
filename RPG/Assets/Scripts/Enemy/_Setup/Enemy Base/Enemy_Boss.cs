using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Enemy_Boss : Enemy
{
    public enum BossType
    {
        None,
        DeathBringer,
        CatWarrior,
        Wizard,
        Black_Knight,
        DragonWarrior
    }

    public enum Stage
    {
        WaitingToStart,
        Stage_1,
        Stage_2,
        Stage_3,
        EndStage
    }

    [Header("Boss info")]
    public EnemyData_Boss enemyData;

    //public string bossName;
    public bool bossFightStart;
    public BoxCollider2D arena;
    //public bool bossIsDefeated;
    //public string bossId;

    public Stage stage;
    private bool callHealthBarOnce;

    [Header("Teleport Details")]
    [SerializeField] private Vector2 surroundingCheckSize;
    public float chanceToTeleport;
    public float defaultChanceToTeleport = 25;
    [SerializeField] private float teleportCooldown = 1.5f;
    private float teleportCooldownTimer = 0;

    protected override void Awake()
    {
        base.Awake();

        callHealthBarOnce = true;

        stage = Stage.WaitingToStart;
    }

    protected override void Start()
    {
        base.Start();

        StartCoroutine(CheckWithDelay());
    }

    protected override void Update()
    {
        base.Update();
        teleportCooldownTimer -= Time.deltaTime;

        CheckToStartFight();
        ChangeStages();
    }

    public override void Die()
    {
        base.Die();

        enemyData.isDead = true;
        BossHealthBarManager.instance.BossFightOver();
        EnemyManager.instance.UpdateBosses();
        UnlockEquipment();
    }

    public virtual bool CheckToStartFight()
    {
        if (arena.GetComponent<EnemyZone>().PlayerInZone)
        {
            bossFightStart = true;
            if (callHealthBarOnce)
            {
                BossHealthBarManager.instance.BossFightStart(this);
                callHealthBarOnce = false;
            }
            
        }

        return bossFightStart;
    }

    private IEnumerator CheckWithDelay()
    {
        yield return new WaitForSeconds(.1f);

        CheckIfBossIsDefeated();
    }

    private void CheckIfBossIsDefeated()
    {
        if (enemyData.isDead)
        {        
            Destroy(transform.parent.gameObject);
        }
    }

    public virtual void KillSpawnEnemies()
    {

    }

    public override void SelfDestroy() => Destroy(transform.parent.gameObject, 4f);

    public virtual void UnlockEquipment()
    {
        
    }


    private void ChangeStages()
    {
        switch (stage)
        {
            case Stage.Stage_1:
                if (stats.CalculateHealthPercentages() <= .7f)
                {
                    //enemy under 70% health
                    StartNextStage();
                }
                break;
            case Stage.Stage_2:
                if (stats.CalculateHealthPercentages() <= .5f)
                {
                    //enemy under 50% health
                    StartNextStage();
                }
                break;
            case Stage.Stage_3:
                if (stats.CalculateHealthPercentages() <= .3f)
                {
                    //enemy under 30% health
                    StartNextStage();
                }
                break;
            case Stage.EndStage:              
                break;
        }
    }

    public virtual void StartNextStage()
    {
        switch (stage)
        {
            case Stage.WaitingToStart:
                stage = Stage.Stage_1;
                break;
            case Stage.Stage_1:
                stage = Stage.Stage_2;
                break;
            case Stage.Stage_2:
                stage = Stage.Stage_3;
                break;
            case Stage.Stage_3:
                stage = Stage.EndStage;
                break;
            case Stage.EndStage:
                break;

        }      
    }

    public virtual void FindPosition()
    {
        //add new teleport for attack to this if check
        if (CurrentAbilityEntry != null)
        {
            Transform playerTransform = PlayerUtils.GetPlayerSafe()?.transform;
            if (playerTransform != null)
            {
                const float epsilon = 0.1f;

                float targetDistance =
                    (CurrentAbilityEntry.rangeMax.HasValue && CurrentAbilityEntry.rangeMax.Value > 0f)
                        ? Mathf.Max(0f, CurrentAbilityEntry.rangeMax.Value - epsilon)
                        : (CurrentAbilityEntry.rangeMin.HasValue && CurrentAbilityEntry.rangeMin.Value > 0f)
                            ? CurrentAbilityEntry.rangeMin.Value + epsilon
                            : 2f; 

                int initialSide = Random.value < 0.5f ? -1 : 1;
                Vector2 playerPos = playerTransform.position;

                for (int attempt = 0; attempt < 3; attempt++)
                {
                    int side = (attempt == 0) ? initialSide : (attempt == 1 ? -initialSide : initialSide);
                    float distanceThisAttempt = (attempt < 2) ? targetDistance : Mathf.Max(0.5f, targetDistance * 0.7f);

                    float x = playerPos.x + side * distanceThisAttempt;
                    float y = playerPos.y;

                    transform.position = new Vector3(x, y);
                    transform.position = new Vector3(
                        transform.position.x,
                        transform.position.y - GroundBelow().distance + (cd.size.y / 2)
                    );

                    if (GroundBelow() && !SomethingIsAround())
                    {
                        return;
                    }
                }
            }
        }

        float xRand = Random.Range(arena.bounds.min.x + 3, arena.bounds.max.x - 3);
        float yRand = Random.Range(arena.bounds.min.y + 3, arena.bounds.max.y - 3);

        transform.position = new Vector3(xRand, yRand);
        transform.position = new Vector3(transform.position.x, transform.position.y - GroundBelow().distance + (cd.size.y / 2));

        if (!GroundBelow() && !SomethingIsAround())
        {
            FindPosition();
        }
    }


    public virtual void MoveToPosition()
    {

    }

    private RaycastHit2D GroundBelow() => Physics2D.Raycast(transform.position, Vector2.down, 100, whatIsGround);
    private bool SomethingIsAround() => Physics2D.BoxCast(transform.position, surroundingCheckSize, 0, Vector2.zero, 0, whatIsGround);

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y - GroundBelow().distance));
        Gizmos.DrawWireCube(transform.position, surroundingCheckSize);
    }

    public virtual bool CanTeleport()
    {
        if (teleportCooldownTimer < 0)
        {
            if (Random.Range(0, 100) <= chanceToTeleport)
            {
                chanceToTeleport = defaultChanceToTeleport;
                teleportCooldownTimer = teleportCooldown;
                return true;
            }
        }
        return false;
    }
}
