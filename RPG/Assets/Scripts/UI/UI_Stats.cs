using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stats : MonoBehaviour
{
    private CharacterStats stats;
    private int statsPoints;
    private int startingPoints;
    private int pointsToAdd;

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI statsPointText;
    [SerializeField] private Slider xpBarSlider;
    [SerializeField] private TextMeshProUGUI xpText;

    #region stats

    [Header("Health")]
    [SerializeField] private TextMeshProUGUI healthNumberText;
    private int health;

    [Space]
    [Header("Damage")]
    [SerializeField] private TextMeshProUGUI damageNumberText;
    private int damage;

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
    private int critChance;

    [Space]
    [Header("CritPower")]
    [SerializeField] private TextMeshProUGUI critPowerNumberText;
    private int critPower;

    [Space]
    [Header("Armor")]
    [SerializeField] private TextMeshProUGUI armorNumberText;
    [SerializeField] private TextMeshProUGUI armorPointsText;
    private int armor;
    private int armorPoint;

    [Space]
    [Header("Evasion")]
    [SerializeField] private TextMeshProUGUI evasionNumberText;
    private int evasion;

    [Space]
    [Header("MagicResistance")]
    [SerializeField] private TextMeshProUGUI magicResistanceNumberText;
    private int magicResistance;

    [Space]
    [Header("FireDamage")]
    [SerializeField] private TextMeshProUGUI fireDamageNumberText;
    private int fireDamage;


    [Space]
    [Header("IceDamage")]
    [SerializeField] private TextMeshProUGUI iceDamageNumberText;
    private int iceDamage;

    [Space]
    [Header("LightingDamage")]
    [SerializeField] private TextMeshProUGUI lightingDamageNumberText;
    private int lightingDamage;

    #endregion

    private void Update()
    {
        statsPointText.text = statsPoints.ToString();

        startingPoints = PlayerManager.instance.statsPoints;

        SetupStats();
    }

    public void StatsCalled()
    {
        stats = PlayerManager.instance.player.stats;
        ZeroOutStats();
        SetupStats();

        statsPoints = PlayerManager.instance.statsPoints;
        startingPoints = statsPoints;

        float xpCurrent = PlayerManager.instance.currentXp;
        float xpRequired = PlayerManager.instance.requiredXp;
        levelText.text = PlayerManager.instance.level.ToString();

        xpText.text = xpCurrent + "/ " + xpRequired;
        if (xpText.text.Length > 14)
            xpText.fontSize = xpText.fontSize * .7f;
        else
            xpText.fontSize = 36;

        xpBarSlider.value = xpCurrent / xpRequired;
    }

    public void UpdateStartingPoints(int _points)
    {
        statsPoints += _points;
    }

    private void SetupStatText()
    {
        healthNumberText.text = health.ToString();
        damageNumberText.text = damage.ToString();
        strengthNumberText.text = strength.ToString();
        strengthPointsText.text = strengthPoint.ToString();
        agilityNumberText.text = agility.ToString();
        agilityPointsText.text = agilityPoint.ToString();
        intelligenceNumberText.text = intelligence.ToString();
        intelligencePointsText.text = intelligencePoint.ToString();
        vitalityNumberText.text = vitality.ToString();
        vitalityPointsText.text = vitalityPoint.ToString();
        critChanceNumberText.text = critChance.ToString();
        critPowerNumberText.text = critPower.ToString();
        armorNumberText.text = armor.ToString();
        armorPointsText.text = armorPoint.ToString();
        evasionNumberText.text = evasion.ToString();
        magicResistanceNumberText.text = magicResistance.ToString();
        fireDamageNumberText.text = fireDamage.ToString();
        iceDamageNumberText.text = iceDamage.ToString();
        lightingDamageNumberText.text = lightingDamage.ToString();
    }

    private void SetupStats()
    {
        strength = stats.strength.GetBaseValue() + strengthPoint;
        armor = stats.armor.GetBaseValue() + armorPoint;
        intelligence = stats.intelligence.GetBaseValue() + intelligencePoint;
        agility = stats.agility.GetBaseValue() + agilityPoint;
        vitality = stats.vitality.GetBaseValue() + vitalityPoint;
        
        health = stats.maxHealth.GetBaseValue() + vitality * 5;
        damage = stats.damage.GetBaseValue() + strength;
        critChance = Mathf.RoundToInt((stats.critChance.GetBaseValue() + strength) *.01f + agility);
        critPower = Mathf.RoundToInt((stats.critPower.GetBaseValue() + strength) * .01f);
        evasion = stats.evasion.GetBaseValue() + agility;
        magicResistance = stats.magicResistance.GetBaseValue() + (intelligence * 3);
        fireDamage = stats.fireDamage.GetBaseValue();
        iceDamage = stats.iceDamage.GetBaseValue();
        lightingDamage = stats.lightingDamage.GetBaseValue();
        Debug.Log("crit power from stats " + stats.critPower.GetValue());
        Debug.Log("crit power " + critPower);
        SetupStatText();
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
        strengthPoint = 0;
        agilityPoint = 0;
        intelligencePoint = 0;
        vitalityPoint = 0;
        armorPoint = 0;

        strengthNumberText.color = Color.white;
        strengthPointsText.color = Color.white;
        critPowerNumberText.color = Color.white;
        critChanceNumberText.color = Color.white;
        agilityPointsText.color = Color.white;
        agilityNumberText.color = Color.white;
        evasionNumberText.color = Color.white;
        intelligencePointsText.color = Color.white;
        intelligenceNumberText.color = Color.white;
        magicResistanceNumberText.color = Color.white;
        vitalityNumberText.color = Color.white;
        vitalityPointsText.color = Color.white;
        healthNumberText.color = Color.white;
        armorPointsText.color = Color.white;
        armorNumberText.color = Color.white;
        damageNumberText.color = Color.white;
    }

    private void UpdateStats()
    {
        stats.strength.SetDefaultValue(stats.strength.GetBaseValue() + strengthPoint);
        stats.agility.SetDefaultValue(stats.agility.GetBaseValue() + agilityPoint);
        stats.intelligence.SetDefaultValue(stats.intelligence.GetBaseValue() + intelligencePoint);
        stats.vitality.SetDefaultValue(stats.vitality.GetBaseValue() + vitalityPoint);
        stats.armor.SetDefaultValue(stats.armor.GetBaseValue() + armorPoint);

    }

    public void IncreasedStats(string _stats)
    {
        if (pointsToAdd < startingPoints)
        {


            if (_stats == "Strength")
            {
                pointsToAdd++;
                statsPoints--;
                strengthPoint++;
                strength++;
                if (strengthPoint > 0)
                {
                    strengthPointsText.color = Color.green;
                    strengthNumberText.color = Color.green;
                    critChanceNumberText.color = Color.green;
                    critPowerNumberText.color = Color.green;
                    damageNumberText.color = Color.green;
                }
            }

            if (_stats == "Agility")
            {
                pointsToAdd++;
                statsPoints--;
                agilityPoint++;
                agility++;
                if (agilityPoint > 0)
                {
                    agilityPointsText.color = Color.green;
                    agilityNumberText.color = Color.green;
                    critChanceNumberText.color = Color.green;
                    evasionNumberText.color = Color.green;
                }
            }

            if (_stats == "Intelligence")
            {
                pointsToAdd++;
                statsPoints--;
                intelligencePoint++;
                intelligence++;
                if (intelligencePoint > 0)
                {
                    intelligencePointsText.color = Color.green;
                    intelligenceNumberText.color = Color.green;
                    magicResistanceNumberText.color = Color.green;
                }
            }

            if (_stats == "Vitality")
            {
                pointsToAdd++;
                statsPoints--;
                vitalityPoint++;
                vitality++;
                if (vitalityPoint > 0)
                {
                    vitalityNumberText.color = Color.green;
                    vitalityPointsText.color = Color.green;
                    healthNumberText.color = Color.green;
                }
            }


            if (_stats == "Armor")
            {
                pointsToAdd++;
                statsPoints--;
                armorPoint++;
                armor++;
                if (armorPoint > 0)
                {
                    armorPointsText.color = Color.green;
                    armorNumberText.color = Color.green;
                }
            }
        }
    }

    public void DecreasedStats(string _stats)
    {


        if (_stats == "Strength")
        {
            if (strengthPoint > 0)
            {
                pointsToAdd--;
                statsPoints++;
                strengthPoint--;
                strength--;

                if (strengthPoint <= 0)
                {
                    strengthNumberText.color = Color.white;
                    strengthPointsText.color = Color.white;
                    critPowerNumberText.color = Color.white;
                    damageNumberText.color = Color.white;
                    if(agilityPoint <= 0)
                        critChanceNumberText.color = Color.white;
                }
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
                if (agilityPoint <= 0)
                {
                    agilityPointsText.color = Color.white;
                    agilityNumberText.color = Color.white;
                    evasionNumberText.color = Color.white;
                    if(strengthPoint <= 0)
                        critChanceNumberText.color = Color.white;
                }
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
                if (intelligencePoint <= 0)
                {
                    intelligencePointsText.color = Color.white;
                    intelligenceNumberText.color = Color.white;
                    magicResistanceNumberText.color = Color.white;
                }
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
                if (vitalityPoint <= 0)
                {
                    vitalityNumberText.color = Color.white;
                    vitalityPointsText.color = Color.white;
                    healthNumberText.color = Color.white;
                }
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
                if (armorPoint <= 0)
                {
                    armorPointsText.color = Color.white;
                    armorNumberText.color = Color.white;
                }
            }
        }
    }
}
