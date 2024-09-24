using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData 
{
    public int skillsPoints;
    public int statsPoints;
    public int level;
    public float currentXp;
    public float requiredXp;

    public int maxHealth;
    public int damage;
    public int strength;
    public int agility;
    public int intelligence;
    public int vitality;
    public int critChance;
    public int critPower;
    public int armor;
    public int evasion;
    public int magicResistance;
    public int fireDamage;
    public int iceDamage;
    public int lightingDamage;

    public SerializableDictionary<string, bool> skillTree;
    public SerializableDictionary<string, int> inventory;
    public List<string> equipmentId;

    public SerializableDictionary<string, bool> checkpoints;
    public string closestCheckpointId;

    public SerializableDictionary<string, float> volumeSettings;

    public GameData()
    {
        skillsPoints = 0;
        maxHealth = 100;
        damage = 50;

        skillTree = new SerializableDictionary<string, bool>();
        inventory = new SerializableDictionary<string, int>();
        equipmentId = new List<string>();

        closestCheckpointId = string.Empty;
        checkpoints = new SerializableDictionary<string, bool>();

        volumeSettings = new SerializableDictionary<string, float>();
    }
}
