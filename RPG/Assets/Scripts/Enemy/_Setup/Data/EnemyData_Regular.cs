using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Archer,
    Crab,
    Jumper,
    Knight,
    Octopus,
    PiranhaPlant,
    Shady,
    Skeleton,
    Slime,
    Slug
}

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Data/Enemy/Regular")]
public class EnemyData_Regular : EnemyData
{
    public EnemyType enemyType;
    public bool canBeSummon;
}
