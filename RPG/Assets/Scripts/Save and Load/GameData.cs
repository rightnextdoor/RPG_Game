using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData 
{
    public int skillsPoints;
    public int level;
    public float currentXp;
    public float requiredXp;

    public SerializableDictionary<string, bool> skillTree;
    public SerializableDictionary<string, int> inventory;
    public List<string> equipmentId;

    public SerializableDictionary<string, bool> checkpoints;
    public string closestCheckpointId;

    public SerializableDictionary<string, float> volumeSettings;

    public GameData()
    {
        skillTree = new SerializableDictionary<string, bool>();
        inventory = new SerializableDictionary<string, int>();
        equipmentId = new List<string>();

        closestCheckpointId = string.Empty;
        checkpoints = new SerializableDictionary<string, bool>();

        volumeSettings = new SerializableDictionary<string, float>();
    }
}
