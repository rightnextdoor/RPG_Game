using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Stats : MonoBehaviour
{
    private CharacterStats stats;
    private int statsPoints;
    private int startingPoints;
    private int pointsToAdd;

    [SerializeField] private TextMeshProUGUI statsPointText;

    #region stats

    [Header("Health")]
    [SerializeField] private TextMeshProUGUI healthNumberText;
    [SerializeField] private TextMeshProUGUI healthPointsText;
    private int health;
    private int healthPoint;

    [Space]
    [Header("Damage")]
    [SerializeField] private TextMeshProUGUI damageNumberText;
    [SerializeField] private TextMeshProUGUI damagePointsText;
    private int damage;
    private int damagePoint;

    [Space]
    [Header("Strength")]
    [SerializeField] private TextMeshProUGUI strengthNumberText;
    [SerializeField] private TextMeshProUGUI strengthPointsText;
    private int strength;
    private int strengthPoint;

    [Space]
    [Header("Agility")]
    [SerializeField] private TextMeshProUGUI agilityNumberText;
    [SerializeField] private TextMeshProUGUI agilityPointsText;
    private int agility;
    private int agilityPoint;

    [Space]
    [Header("Intelligence")]
    [SerializeField] private TextMeshProUGUI intelligenceNumberText;
    [SerializeField] private TextMeshProUGUI intelligencePointsText;
    private int intelligence;
    private int intelligencePoint;

    [Space]
    [Header("Vitality")]
    [SerializeField] private TextMeshProUGUI vitalityNumberText;
    [SerializeField] private TextMeshProUGUI vitalityPointsText;
    private int vitality;
    private int vitalityPoint;

    [Space]
    [Header("CritChance")]
    [SerializeField] private TextMeshProUGUI critChanceNumberText;
    [SerializeField] private TextMeshProUGUI critChancePointsText;
    private int critChance;
    private int critChancePoint;

    [Space]
    [Header("CritPower")]
    [SerializeField] private TextMeshProUGUI critPowerNumberText;
    [SerializeField] private TextMeshProUGUI critPowerPointsText;
    private int critPower;
    private int critPowerPoint;

    [Space]
    [Header("Armor")]
    [SerializeField] private TextMeshProUGUI armorNumberText;
    [SerializeField] private TextMeshProUGUI armorPointsText;
    private int armor;
    private int armorPoint;

    [Space]
    [Header("Evasion")]
    [SerializeField] private TextMeshProUGUI evasionNumberText;
    [SerializeField] private TextMeshProUGUI evasionPointsText;
    private int evasion;
    private int evasionPoint;

    [Space]
    [Header("MagicResistance")]
    [SerializeField] private TextMeshProUGUI magicResistanceNumberText;
    [SerializeField] private TextMeshProUGUI magicResistancePointsText;
    private int magicResistance;
    private int magicResistancePoint;

    [Space]
    [Header("FireDamage")]
    [SerializeField] private TextMeshProUGUI fireDamageNumberText;
    [SerializeField] private TextMeshProUGUI fireDamagePointsText;
    private int fireDamage;
    private int fireDamagePoint;

    [Space]
    [Header("IceDamage")]
    [SerializeField] private TextMeshProUGUI iceDamageNumberText;
    [SerializeField] private TextMeshProUGUI iceDamagePointsText;
    private int iceDamage;
    private int iceDamagePoint;

    [Space]
    [Header("LightingDamage")]
    [SerializeField] private TextMeshProUGUI lightingDamageNumberText;
    [SerializeField] private TextMeshProUGUI lightingDamagePointsText;
    private int lightingDamage;
    private int lightingDamagePoint;

    #endregion

    private void Start()
    {
        stats = PlayerManager.instance.player.stats;
        ZeroOutStats();
        SetupStats();
    }

    

    private void Update()
    {
        statsPoints = PlayerManager.instance.statsPoints;
        startingPoints = statsPoints;

        statsPointText.text = "Stats Points: " + statsPoints;

        SetupStatText();

        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayerManager.instance.statsPoints += 5;
        }
    }

    private void SetupStatText()
    {
        healthNumberText.text = health.ToString();
        healthPointsText.text = healthPoint.ToString();
        damageNumberText.text = damage.ToString();
        damagePointsText.text = damagePoint.ToString();
        strengthNumberText.text = strength.ToString();
        strengthPointsText.text = strengthPoint.ToString();
        agilityNumberText.text = agility.ToString();
        agilityPointsText.text = agilityPoint.ToString();
        intelligenceNumberText.text = intelligence.ToString();
        intelligencePointsText.text = intelligencePoint.ToString();
        vitalityNumberText.text = vitality.ToString();
        vitalityPointsText.text = vitalityPoint.ToString();
        critChanceNumberText.text = critChance.ToString();
        critChancePointsText.text = critChancePoint.ToString();
        critPowerNumberText.text = critPower.ToString();
        critPowerPointsText.text = critPowerPoint.ToString();
        armorNumberText.text = armor.ToString();
        armorPointsText.text = armorPoint.ToString();
        evasionNumberText.text = evasion.ToString();
        evasionPointsText.text = evasionPoint.ToString();
        magicResistanceNumberText.text = magicResistance.ToString();
        magicResistancePointsText.text = magicResistancePoint.ToString();
        fireDamageNumberText.text = fireDamage.ToString();
        fireDamagePointsText.text = fireDamagePoint.ToString();
        iceDamageNumberText.text = iceDamage.ToString();
        iceDamagePointsText.text = iceDamagePoint.ToString();
        lightingDamageNumberText.text = lightingDamage.ToString();
        lightingDamagePointsText.text = lightingDamagePoint.ToString();
    }

    private void SetupStats()
    {
        health = stats.GetMaxHealthValue();
        damage = stats.damage.GetValue();
        strength = stats.strength.GetValue();
        agility = stats.agility.GetValue();
        intelligence = stats.intelligence.GetValue();
        vitality = stats.vitality.GetValue();
        critChance = stats.critChance.GetValue();
        critPower = stats.critPower.GetValue();
        armor = stats.armor.GetValue();
        evasion = stats.evasion.GetValue();
        magicResistance = stats.magicResistance.GetValue();
        fireDamage = stats.fireDamage.GetValue();
        iceDamage = stats.iceDamage.GetValue();
        lightingDamage = stats.lightingDamage.GetValue();
    }

    public void ApplyStats()
    {
        PlayerManager.instance.HaveEnoughStatsPoints(pointsToAdd);
        UpdateStats();

        ZeroOutStats();
    }

    private void ZeroOutStats()
    {
        pointsToAdd = 0;
        healthPoint = 0;
        damagePoint = 0;
        strengthPoint = 0;
        agilityPoint = 0;
        intelligencePoint = 0;
        vitalityPoint = 0;
        critChancePoint = 0;
        critPowerPoint = 0;
        armorPoint = 0;
        evasionPoint = 0;
        magicResistancePoint = 0;
        fireDamagePoint = 0;
        iceDamagePoint = 0;
        lightingDamagePoint = 0;
    }

    private void UpdateStats()
    {
        stats.maxHealth.SetDefaultValue(healthPoint + stats.maxHealth.GetValue());
        stats.currentHealth += healthPoint;
        stats.damage.SetDefaultValue(damagePoint + stats.damage.GetValue());
        stats.strength.SetDefaultValue(stats.strength.GetValue() + startingPoints);
        stats.agility.SetDefaultValue(stats.agility.GetValue() + agilityPoint);
        stats.intelligence.SetDefaultValue(stats.intelligence.GetValue() + intelligencePoint);
        stats.vitality.SetDefaultValue(stats.vitality.GetValue() + vitalityPoint);
        stats.critChance.SetDefaultValue(stats.critChance.GetValue() + critChancePoint);
        stats.critPower.SetDefaultValue(stats.critPower.GetValue() + critPowerPoint);
        stats.armor.SetDefaultValue(stats.armor.GetValue() + armorPoint);
        stats.evasion.SetDefaultValue(stats.evasion.GetValue() + evasionPoint);
        stats.magicResistance.SetDefaultValue(stats.magicResistance.GetValue() + magicResistancePoint);
        stats.fireDamage.SetDefaultValue(stats.fireDamage.GetValue() + fireDamagePoint);
        stats.iceDamage.SetDefaultValue(stats.iceDamage.GetValue() + iceDamagePoint);
        stats.lightingDamage.SetDefaultValue(stats.lightingDamage.GetValue() + lightingDamagePoint);
    }

    public void IncreasedStats(string _stats)
    {
        if (pointsToAdd < startingPoints)
        {
            if (_stats == "Health")
            {
                pointsToAdd++;
                statsPoints--;
                healthPoint++;
                health++;
            }

            if (_stats == "Damage")
            {
                pointsToAdd++;
                statsPoints--;
                damagePoint++;
                damage++;
            }

            if (_stats == "Strength")
            {
                pointsToAdd++;
                statsPoints--;
                strengthPoint++;
                strength++;
            }

            if (_stats == "Agility")
            {
                pointsToAdd++;
                statsPoints--;
                agilityPoint++;
                agility++;
            }

            if (_stats == "Intelligence")
            {
                pointsToAdd++;
                statsPoints--;
                intelligencePoint++;
                intelligence++;
            }

            if (_stats == "Vitality")
            {
                pointsToAdd++;
                statsPoints--;
                vitalityPoint++;
                vitality++;
            }

            if (_stats == "CritChance")
            {
                pointsToAdd++;
                statsPoints--;
                critChancePoint++;
                critChance++;
            }

            if (_stats == "CritPower")
            {
                pointsToAdd++;
                statsPoints--;
                critPowerPoint++;
                critPower++;
            }

            if (_stats == "Armor")
            {
                pointsToAdd++;
                statsPoints--;
                armorPoint++;
                armor++;
            }

            if (_stats == "Evasion")
            {
                pointsToAdd++;
                statsPoints--;
                evasionPoint++;
                evasion++;
            }

            if (_stats == "MagicResistance")
            {
                pointsToAdd++;
                statsPoints--;
                magicResistancePoint++;
                magicResistance++;
            }

            if (_stats == "FireDamage")
            {
                pointsToAdd++;
                statsPoints--;
                fireDamagePoint++;
                fireDamage++;
            }

            if (_stats == "IceDamage")
            {
                pointsToAdd++;
                statsPoints--;
                iceDamagePoint++;
                iceDamage++;
            }

            if (_stats == "LightingDamage")
            {
                pointsToAdd++;
                statsPoints--;
                lightingDamagePoint++;
                lightingDamage++;
            }

        }
    }

    public void DecreasedStats(string _stats)
    {
        if (_stats == "Health")
        {
            if (healthPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                healthPoint--;
                health--;
            }

        }
        if (_stats == "Damage")
        {
            if (damagePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                damagePoint--;
                damage--;
            }
        }

        if (_stats == "Strength")
        {
            if (startingPoints > 0)
            {
                pointsToAdd--;
                statsPoints++;
                strengthPoint--;
                strength--;
            }
        }

        if (_stats == "Agility")
        {
            if (agilityPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                agilityPoint--;
                agility--;
            }
        }

        if (_stats == "Intelligence")
        {
            if (intelligencePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                intelligencePoint--;
                intelligence--;
            }
        }

        if (_stats == "Vitality")
        {
            if (vitalityPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                vitalityPoint--;
                vitality--;
            }
        }

        if (_stats == "CritChance")
        {
            if (critChancePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                critChancePoint--;
                critChance--;
            }
        }

        if (_stats == "CritPower")
        {
            if (critPowerPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                critPowerPoint--;
                critPower--;
            }
        }

        if (_stats == "Armor")
        {
            if (armorPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                armorPoint--;
                armor--;
            }
        }

        if (_stats == "Evasion")
        {
            if (evasionPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                evasionPoint--;
                evasion--;
            }
        }

        if (_stats == "MagicResistance")
        {
            if (magicResistancePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                magicResistancePoint--;
                magicResistance--;
            }
        }

        if (_stats == "FireDamage")
        {
            if (fireDamagePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                fireDamagePoint--;
                fireDamage--;
            }
        }

        if (_stats == "IceDamage")
        {
            if (iceDamagePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                iceDamagePoint--;
                iceDamage--;
            }
        }

        if (_stats == "LightingDamage")
        {
            if (lightingDamagePoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                lightingDamagePoint--;
                lightingDamage--;
            }
        }
    }
}
