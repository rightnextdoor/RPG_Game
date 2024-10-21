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
    [SerializeField] private GameObject bossHealthBar;

    public Stage stage;

    private bool once;
    private bool callHealthBarOnce;

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

        if (once)
            IsBossDefeated();

        CheckToStartFight();
        ChangeStages();
    }

    public override void Die()
    {
        base.Die();

        bossIsDefeated = true;
        GameManager.instance.UpdateBosses();

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
                bossHealthBar.GetComponent<UI_BossHealthBar>().BossFightStart();
                callHealthBarOnce= false;
            }
            
        }

        return bossFightStart;
    }

    private void IsBossDefeated()
    {
        once = false;

        if (bossIsDefeated)
        {
            KillSpawnEnemies();
            bossHealthBar.GetComponent<UI_BossHealthBar>().BossFightOver();
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

}
