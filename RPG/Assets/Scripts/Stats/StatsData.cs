using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stats Data", menuName = "Data/Stats")]
public class StatsData : ScriptableObject
{
    public string statName;
    public StatType statType;

    [TextArea]
    public string statsDescription;
}
