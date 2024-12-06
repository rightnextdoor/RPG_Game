using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Connection", menuName = "Levels/Connection")]
public class LevelConnection : ScriptableObject
{
    public static LevelConnection ActiveConnection {  get; set; }
}
