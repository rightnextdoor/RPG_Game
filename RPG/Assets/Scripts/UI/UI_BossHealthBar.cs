using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_BossHealthBar : MonoBehaviour
{
    [SerializeField] private Enemy_Boss boss;
    private CharacterStats enemy;
    [SerializeField] private GameObject bossHealthBar;

    [Header("HealthBar")]
    [SerializeField] private Slider sliderFrontBar;
    [SerializeField] private Slider sliderBackbar;
    [SerializeField] private Image backHealhBarImage;
    [SerializeField] private float chipSpeed = 10;
    [SerializeField] private TextMeshProUGUI bossNameText;

    private void Update()
    {
        UpdateHealthUI();
    }

    public void BossFightStart()
    {
        bossHealthBar.gameObject.SetActive(true);
        enemy = boss.stats;

        sliderFrontBar.maxValue = enemy.GetMaxHealthValue();
        sliderBackbar.maxValue = enemy.GetMaxHealthValue();
        sliderFrontBar.value = enemy.currentHealth;
        sliderBackbar.value = enemy.currentHealth;
        bossNameText.text = boss.bossName;
    }

    public void BossFightOver()
    {
        bossHealthBar.gameObject.SetActive(false);
    }

    private void UpdateHealthUI()
    {
        float fillF = sliderFrontBar.value;
        float fillB = sliderBackbar.value;

        if (fillB > enemy.currentHealth)
        {
            sliderFrontBar.value = enemy.currentHealth;
            backHealhBarImage.color = Color.red;
            enemy.lerpTimer += Time.deltaTime;
            float percentComplete = enemy.lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            sliderBackbar.value = Mathf.Lerp(fillB, enemy.currentHealth, percentComplete);
        }

        if (fillF < enemy.currentHealth)
        {
            backHealhBarImage.color = Color.green;
            sliderBackbar.value = enemy.currentHealth;
            enemy.lerpTimer += Time.deltaTime;
            float percentComplete = enemy.lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            sliderFrontBar.value = Mathf.Lerp(fillF, enemy.currentHealth, percentComplete);
        }
    }

}
