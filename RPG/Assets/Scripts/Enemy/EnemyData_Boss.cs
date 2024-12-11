using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enemy_Boss;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Data/Enemy/Boss")]
public class EnemyData_Boss : EnemyData
{
    public string bossName;
    public BossType bossType;
}
