using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [Header("HealthBar")]
    [SerializeField] private Slider sliderFrontBar;
    [SerializeField] private Slider sliderBackbar;
    [SerializeField] private Image backHealhBarImage;
    [SerializeField] private float chipSpeed = 10;
    [SerializeField] private TextMeshProUGUI healthText;

    [Space]
    [SerializeField] private Image dashImage;
    [SerializeField] private Image parryImage;
    [SerializeField] private Image crystalImage;
    [SerializeField] private Image swordImage;
    [SerializeField] private Image blackHoleImage;
    [SerializeField] private Image flaskImage;
    [SerializeField] private Image flaskCooldownImage;
    private SkillManager skills;

    private bool setupValue = false;

    void Start()
    {
        if (playerStats != null)
            playerStats.onHealthChanged += UpdateHealthUI;

        skills = SkillManager.instance;

        setupValue = true;
    }

    void Update()
    {
        UpdateHealthUI();
       
        ItemData_Equipment flask = Inventory.instance.GetEquipment(EquipmentType.Flask);
        if (flask != null)
        {
            flaskImage.sprite = flask.itemIcon;
            flaskCooldownImage.sprite = flask.itemIcon;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && skills.dash.dashUnlocked)
            SetCooldownOf(dashImage);

        if (Input.GetKeyDown(KeyCode.Q) && skills.parry.parryUnlocked)
            SetCooldownOf(parryImage);

        if (Input.GetKeyDown(KeyCode.F) && skills.crystal.crystalUnlocked)
            SetCooldownOf(crystalImage);

        if (Input.GetKeyDown(KeyCode.Mouse1) && skills.sword.swordUnlocked)
            SetCooldownOf(swordImage);

        if (Input.GetKeyDown(KeyCode.R) && skills.blackhole.blackholeUnlocked)
            SetCooldownOf(blackHoleImage);

        if (Input.GetKeyDown(KeyCode.Alpha1) && flask != null)
        {
            SetCooldownOf(flaskCooldownImage);
        }

        CheckCooldownOf(dashImage, skills.dash.cooldown);
        CheckCooldownOf(parryImage, skills.parry.cooldown);
        CheckCooldownOf(crystalImage, skills.crystal.cooldown);
        CheckCooldownOf(swordImage, skills.sword.cooldown);
        CheckCooldownOf(blackHoleImage, skills.blackhole.cooldown);

        CheckCooldownOf(flaskCooldownImage, Inventory.instance.flaskCooldown);
    }
    private void UpdateHealthUI()
    {
        sliderFrontBar.maxValue = playerStats.GetMaxHealthValue();
        sliderBackbar.maxValue = playerStats.GetMaxHealthValue();
        
        if (setupValue)
        {
            SetupCurrentValue();
            setupValue = false;
        }

        float fillF = sliderFrontBar.value;
        float fillB = sliderBackbar.value;

        if (fillB > playerStats.currentHealth)
        {
            sliderFrontBar.value = playerStats.currentHealth;
            backHealhBarImage.color = Color.red;
            playerStats.lerpTimer += Time.deltaTime;
            float percentComplete = playerStats.lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            sliderBackbar.value = Mathf.Lerp(fillB, playerStats.currentHealth, percentComplete);
        }

        if (fillF < playerStats.currentHealth)
        {
            backHealhBarImage.color = Color.green;
            sliderBackbar.value = playerStats.currentHealth;
            playerStats.lerpTimer += Time.deltaTime;
            float percentComplete = playerStats.lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            sliderFrontBar.value = Mathf.Lerp(fillF, playerStats.currentHealth, percentComplete);
        }
        healthText.text = Mathf.Round(playerStats.currentHealth) + "/" + Mathf.Round(playerStats.GetMaxHealthValue());
    }

    private void SetupCurrentValue()
    {
        sliderFrontBar.value = playerStats.currentHealth;
        sliderBackbar.value = playerStats.currentHealth;
    }

    private void SetCooldownOf(Image _image)
    {
        if (_image.fillAmount <= 0)
            _image.fillAmount = 1;
    }

    private void CheckCooldownOf(Image _image, float _cooldown)
    {
        if (_image.fillAmount > 0)
            _image.fillAmount -= 1 / _cooldown * Time.deltaTime;
    }
}
