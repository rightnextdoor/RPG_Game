using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    private PlayerStats playerStats;
    private SkillManager skills;

    [Header("HealthBar")]
    [SerializeField] private Slider sliderFrontBar;
    [SerializeField] private Slider sliderBackbar;
    [SerializeField] private Image backHealhBarImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private float chipSpeed = 10f;

    [Header("Cooldown UI")]
    [SerializeField] private Image dashImage;
    [SerializeField] private Image parryImage;
    [SerializeField] private Image crystalImage;
    [SerializeField] private Image swordImage;
    [SerializeField] private Image blackHoleImage;
    [SerializeField] private Image flaskImage;
    [SerializeField] private Image flaskCooldownImage;

    private bool setupValue = false;
    private bool initialized = false;

    private void Start()
    {
        StartCoroutine(WaitForPlayerAndInitialize());
    }

    private IEnumerator WaitForPlayerAndInitialize()
    {
        yield return new WaitUntil(() => PlayerManager.instance?.player != null);
        playerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();
        skills = SkillManager.instance;

        if (playerStats != null)
            playerStats.onHealthChanged += UpdateHealthUI;

        setupValue = true;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        UpdateHealthUI();

        var flask = Inventory.instance.GetEquipment(EquipmentType.Flask);
        if (flask != null)
        {
            if (flaskImage != null) flaskImage.sprite = flask.itemIcon;
            if (flaskCooldownImage != null) flaskCooldownImage.sprite = flask.itemIcon;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && skills.dash.dashUnlocked) SetCooldownOf(dashImage);
        if (Input.GetKeyDown(KeyCode.Q) && skills.parry.parryUnlocked) SetCooldownOf(parryImage);
        if (Input.GetKeyDown(KeyCode.F) && skills.crystal.crystalUnlocked && !skills.inBlackholeState) SetCooldownOf(crystalImage);
        if (Input.GetKeyDown(KeyCode.Mouse1) && skills.sword.swordUnlocked) SetCooldownOf(swordImage);
        if (Input.GetKeyDown(KeyCode.R) && skills.blackhole.blackholeUnlocked) SetCooldownOf(blackHoleImage);
        if (Input.GetKeyDown(KeyCode.Alpha1) && flask != null) SetCooldownOf(flaskCooldownImage);

        CheckCooldownOf(dashImage, skills.dash.cooldown);
        CheckCooldownOf(parryImage, skills.parry.cooldown);
        CheckCooldownOf(crystalImage, skills.crystal.cooldown);
        CheckCooldownOf(swordImage, skills.sword.cooldown);
        CheckCooldownOf(blackHoleImage, skills.blackhole.cooldown);
        CheckCooldownOf(flaskCooldownImage, Inventory.instance.flaskCooldown);
    }

    private void UpdateHealthUI()
    {
        if (playerStats == null || sliderFrontBar == null || sliderBackbar == null)
            return;

        sliderFrontBar.maxValue = playerStats.GetMaxHealthValue();
        sliderBackbar.maxValue = playerStats.GetMaxHealthValue();

        if (setupValue)
        {
            sliderFrontBar.value = playerStats.currentHealth;
            sliderBackbar.value = playerStats.currentHealth;
            setupValue = false;
        }

        float frontValue = sliderFrontBar.value;
        float backValue = sliderBackbar.value;

        if (backValue > playerStats.currentHealth)
        {
            sliderFrontBar.value = playerStats.currentHealth;
            if (backHealhBarImage != null) backHealhBarImage.color = Color.red;

            playerStats.lerpTimer += Time.deltaTime;
            float percent = Mathf.Pow(playerStats.lerpTimer / chipSpeed, 2);
            sliderBackbar.value = Mathf.Lerp(backValue, playerStats.currentHealth, percent);
        }

        if (frontValue < playerStats.currentHealth)
        {
            if (backHealhBarImage != null) backHealhBarImage.color = Color.green;
            sliderBackbar.value = playerStats.currentHealth;

            playerStats.lerpTimer += Time.deltaTime;
            float percent = Mathf.Pow(playerStats.lerpTimer / chipSpeed, 2);
            sliderFrontBar.value = Mathf.Lerp(frontValue, playerStats.currentHealth, percent);
        }

        if (healthText != null)
            healthText.text = Mathf.Round(playerStats.currentHealth) + "/" + Mathf.Round(playerStats.GetMaxHealthValue());
    }

    private void SetCooldownOf(Image image)
    {
        if (image != null && image.fillAmount <= 0)
            image.fillAmount = 1;
    }

    private void CheckCooldownOf(Image image, float cooldown)
    {
        if (image != null && image.fillAmount > 0)
            image.fillAmount -= Time.deltaTime / cooldown;
    }
}
