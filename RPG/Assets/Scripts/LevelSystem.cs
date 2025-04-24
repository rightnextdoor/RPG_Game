using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelSystem : MonoBehaviour
{
    private float lerptimer;
    private float delayTimer = 0;
    public int delaySpeed = 4;

    [SerializeField] private int statsPoints = 5;

    [Header("UI References")]
    private Slider frontXpBarSlider;
    private Slider backXpBarSlider;
    private Image backXpBarImage;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI xpText;

    [Header("Multipliers")]
    [Range(1f, 300f)] public float additionMultiplier = 300;
    [Range(2f, 4f)] public float powerMultiplier = 2;
    [Range(7f, 14f)] public float divisionMultiplier = 7;

    private bool initialized = false;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(InitializeOnce());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReassignUI());
    }

    private IEnumerator InitializeOnce()
    {
        yield return new WaitUntil(() =>
            PlayerManager.instance != null &&
            PlayerManager.instance.player != null &&
            UIManager.instance != null &&
            UIManager.instance.GetUILevelSystem() != null
        );

        AssignUIReferences();

        PlayerManager.instance.requiredXp = CalculateRequiredXp();
        UpdateAllUI();

        initialized = true;
    }

    private IEnumerator ReassignUI()
    {
        yield return new WaitUntil(() =>
            UIManager.instance != null &&
            UIManager.instance.GetUILevelSystem() != null
        );

        AssignUIReferences();
        UpdateAllUI();
    }

    private void AssignUIReferences()
    {
        var ui = UIManager.instance.GetUILevelSystem();

        frontXpBarSlider = ui.frontXpBarSlider;
        backXpBarSlider = ui.backXpBarSlider;
        backXpBarImage = ui.backXpBarImage;
        levelText = ui.levelText;
        xpText = ui.xpText;
    }

    private void Update()
    {
        if (!initialized || PlayerManager.instance == null || PlayerManager.instance.player == null)
            return;

        UpdateXpUI();

        if (PlayerManager.instance.currentXp > PlayerManager.instance.requiredXp)
            LevelUp();
    }

    private void UpdateXpUI()
    {
        if (frontXpBarSlider == null || backXpBarSlider == null || xpText == null) return;

        float xpFraction = PlayerManager.instance.currentXp / PlayerManager.instance.requiredXp;
        float FXP = frontXpBarSlider.value;

        if (FXP < xpFraction)
        {
            delayTimer += Time.deltaTime;
            backXpBarSlider.value = xpFraction;
            backXpBarImage.color = Color.green;
            if (delayTimer > 3)
            {
                lerptimer += Time.deltaTime;
                float percentComplete = lerptimer / delaySpeed;
                frontXpBarSlider.value = Mathf.Lerp(FXP, xpFraction, percentComplete);
            }
        }

        xpText.text = Mathf.Round(PlayerManager.instance.currentXp) + "/" + Mathf.Round(PlayerManager.instance.requiredXp);
    }

    private void UpdateAllUI()
    {
        if (frontXpBarSlider == null || backXpBarSlider == null || levelText == null || xpText == null)
            return;

        float fraction = PlayerManager.instance.currentXp / PlayerManager.instance.requiredXp;
        frontXpBarSlider.value = fraction;
        backXpBarSlider.value = fraction;

        levelText.text = "Level " + PlayerManager.instance.GetLevel();
        xpText.text = Mathf.Round(PlayerManager.instance.currentXp) + "/" + Mathf.Round(PlayerManager.instance.requiredXp);
    }

    public void GainExperienceFlatRate(float xpGained)
    {
        PlayerManager.instance.currentXp += xpGained;
        lerptimer = 0f;
        delayTimer = 0f;
    }

    public void GainExperienceScalable(float _xpGained, int _passedLevel)
    {
        float multiplier = _passedLevel < PlayerManager.instance.GetLevel()
            ? 1 + (PlayerManager.instance.GetLevel() - _passedLevel) * 0.1f
            : 1;

        PlayerManager.instance.currentXp += _xpGained * multiplier;
        lerptimer = 0f;
        delayTimer = 0f;
    }

    private void LevelUp()
    {
        PlayerManager.instance.GainLevel();
        PlayerManager.instance.GainSkillsPoints(1);
        PlayerManager.instance.GainStatsPoints(statsPoints);

        PlayerManager.instance.currentXp = Mathf.RoundToInt(PlayerManager.instance.currentXp - PlayerManager.instance.requiredXp);
        PlayerManager.instance.requiredXp = CalculateRequiredXp();

        frontXpBarSlider.value = 0;
        backXpBarSlider.value = 0;
        levelText.text = "Level " + PlayerManager.instance.GetLevel();

        IncreaseStats();
    }

    private void IncreaseStats()
    {
        var stats = PlayerManager.instance.player.stats;

        stats.maxHealth.SetDefaultValue(stats.maxHealth.GetBaseValue() + Random.Range(0, 5));
        stats.damage.SetDefaultValue(stats.damage.GetBaseValue() + Random.Range(0, 5));
        stats.strength.SetDefaultValue(stats.strength.GetBaseValue() + Random.Range(0, 5));
        stats.agility.SetDefaultValue(stats.agility.GetBaseValue() + Random.Range(0, 5));
        stats.intelligence.SetDefaultValue(stats.intelligence.GetBaseValue() + Random.Range(0, 5));
        stats.vitality.SetDefaultValue(stats.vitality.GetBaseValue() + Random.Range(0, 5));
        stats.critChance.SetDefaultValue(stats.critChance.GetBaseValue() + Random.Range(0, 5));
        stats.critPower.SetDefaultValue(stats.critPower.GetBaseValue() + Random.Range(0, 5));
        stats.armor.SetDefaultValue(stats.armor.GetBaseValue() + Random.Range(0, 5));
        stats.evasion.SetDefaultValue(stats.evasion.GetBaseValue() + Random.Range(0, 5));
        stats.magicResistance.SetDefaultValue(stats.magicResistance.GetBaseValue() + Random.Range(0, 2));
    }

    private int CalculateRequiredXp()
    {
        int totalXp = 0;
        for (int levelCycle = 1; levelCycle <= PlayerManager.instance.GetLevel(); levelCycle++)
        {
            totalXp += (int)Mathf.Floor(levelCycle + additionMultiplier * Mathf.Pow(powerMultiplier, levelCycle / divisionMultiplier));
        }
        return totalXp / 4;
    }
}


//example of increasing health by level in characterstats
//public void IncreasedHealth(int level)
//{
//    maxHealth += (health * 0.01f) * ((100 - level) * 0.1f);
//    health = maxHealth;
//}

