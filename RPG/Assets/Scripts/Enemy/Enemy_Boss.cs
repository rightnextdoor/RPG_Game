using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Enemy_Boss : Enemy
{
    public enum Stage
    {
        WaitingToStart,
        Stage_1,
        Stage_2,
        Stage_3,
        EndStage
    }

    [Header("Boss info")]
    public string bossName;
    public bool bossFightStart;
    public BoxCollider2D arena;
    public bool bossIsDefeated;
    public string bossId;

    public Stage stage;

    private bool once;
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
        once = true;
        callHealthBarOnce = true;

        stage = Stage.WaitingToStart;
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        teleportCooldownTimer -= Time.deltaTime;
        if (once)
            CheckIfBossIsDefeated();

        CheckToStartFight();
        ChangeStages();
    }

    public override void Die()
    {
        base.Die();

        bossIsDefeated = true;
        BossHealthBarManager.instance.BossFightOver();

        UnlockEquipment();
    }

    [ContextMenu("Generate boss id")]
    private void GenerateId()
    {
        if(bossId == "")
            bossId = System.Guid.NewGuid().ToString();
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

    private void CheckIfBossIsDefeated()
    {
        once = false;

        if (bossIsDefeated)
        {        
            Destroy(transform.parent.gameObject);
        }
    }

    public virtual void KillSpawnEnemies()
    {

    }

    public void SelfDestroy() => Destroy(transform.parent.gameObject, 2f);

    private void UnlockEquipment()
    {
        UnlockArmor();
        UnlockAmulet();
        UnlockWapon();
    }

    public virtual void UnlockArmor()
    {

    }

    public virtual void UnlockWapon()
    {

    }

    public virtual void UnlockAmulet()
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
        float x = Random.Range(arena.bounds.min.x + 3, arena.bounds.max.x - 3);
        float y = Random.Range(arena.bounds.min.y + 3, arena.bounds.max.y - 3);

        transform.position = new Vector3(x, y);
        transform.position = new Vector3(transform.position.x, transform.position.y - GroundBelow().distance + (cd.size.y / 2));

        if (!GroundBelow() && !SomethingIsAround())
        {
            FindPosition();
        }
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
