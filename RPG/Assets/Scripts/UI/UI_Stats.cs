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

    private int health;
    private int healthPoint;

    private int damage;
    private int damagePoint;

    [SerializeField] private TextMeshProUGUI statsPointText;

    [SerializeField] private TextMeshProUGUI healthNumberText;
    [SerializeField] private TextMeshProUGUI healthPointsText;
    [SerializeField] private TextMeshProUGUI damageNumberText;
    [SerializeField] private TextMeshProUGUI damagePointsText;

    private void Start()
    {
        stats = PlayerManager.instance.player.stats;

        pointsToAdd = 0;
        healthPoint = 0;

        health = stats.GetMaxHealthValue();
        damage = stats.damage.GetValue();
        
        
    }

    private void Update()
    {
        statsPoints = PlayerManager.instance.statsPoints;
        startingPoints = statsPoints;

        statsPointText.text = "Stats Points: " + statsPoints;
        healthNumberText.text = health.ToString();
        healthPointsText.text = healthPoint.ToString();

        damageNumberText.text = damage.ToString();
        damagePointsText.text = damagePoint.ToString();

        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayerManager.instance.statsPoints += 5;
        }
    }

    public void ApplyStats()
    {
        PlayerManager.instance.HaveEnoughStatsPoints(pointsToAdd);

        stats.maxHealth.SetDefaultValue(healthPoint + stats.maxHealth.GetValue());
        stats.currentHealth += healthPoint;
        stats.damage.SetDefaultValue(damagePoint + stats.damage.GetValue());

        healthPoint = 0;
        damagePoint = 0;
        pointsToAdd = 0;
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

        }
    }

    public void DecreasedStats(string _stats)
    {
        if (_stats == "Health")
        {
            if (healthPoint > 0)
            {
                pointsToAdd--;
                health--;
                healthPoint--;
                statsPoints++;
            }

        }
        if (_stats == "Damage")
        {
            if (damagePoint > 0)
            {
                pointsToAdd--;
                damage--;
                damagePoint--;
                statsPoints++;
            }
        }
    }
}
