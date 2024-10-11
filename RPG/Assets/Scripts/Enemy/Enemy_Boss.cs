using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Enemy_Boss : Enemy
{
    [Header("Boss info")]
    public string bossName;
    public bool bossFightStart;
    public BoxCollider2D arena;
    public bool bossIsDefeated;
    public string bossId;

    private bool once;

    protected override void Awake()
    {
        base.Awake();
        once = true;
    }

    protected override void Start()
    {
        base.Start();
        GenerateId();
    }

    protected override void Update()
    {
        base.Update();

        if (once)
            IsBossDefeated();

        CheckToStartFight();
    }

    public override void Die()
    {
        base.Die();

        bossIsDefeated = true;
        GameManager.instance.UpdateBosses();
    }

    [ContextMenu("Generate boss id")]
    private void GenerateId()
    {
        if(bossId == "")
            bossId = System.Guid.NewGuid().ToString();
    }

    private bool CheckToStartFight()
    {
        if (arena.GetComponent<EnemyZone>().PlayerInZone)
        {
            bossFightStart = true;
        }

        return bossFightStart;
    }

    private void IsBossDefeated()
    {
        once = false;

        if (bossIsDefeated)
            Destroy(transform.parent.gameObject);
    }

    public void SelfDestroy() => Destroy(transform.parent.gameObject, 2f);

}
