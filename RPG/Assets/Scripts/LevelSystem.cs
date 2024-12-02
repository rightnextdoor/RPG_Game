using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class LevelSystem : MonoBehaviour
{
    private float lerptimer;
    private float delayTimer = 0;
    public int delaySpeed = 4;
    [SerializeField] private int statsPoints = 5;
    [Header("UI")]
    [SerializeField] private Slider frontXpBarSlider;
    [SerializeField] private Slider backXpBarSlider;
    [SerializeField] private Image backXpBarImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("Multipliers")]
    [Range(1f, 300f)]
    public float additionMultiplier = 300;
    [Range(2f, 4f)]
    public float powerMultiplier = 2;
    [Range(7f, 14f)]
    public float divisionMultiplier = 7;

    private bool start = false;

    void Start()
    {
        start = true;

        PlayerManager.instance.requiredXp = CalculateRequiredXp();
        levelText.text = "Level " + PlayerManager.instance.GetLevel();
    }


    void Update()
    {
        if (start)
        {
            SetUp();
        }

        UpdateXpUI();

        if (PlayerManager.instance.currentXp > PlayerManager.instance.requiredXp)
        {
            LevelUp();
        }
    }

    private void SetUp()
    {
 
        PlayerManager.instance.requiredXp = CalculateRequiredXp();
        frontXpBarSlider.value = PlayerManager.instance.currentXp / PlayerManager.instance.requiredXp;
        backXpBarSlider.value = PlayerManager.instance.currentXp / PlayerManager.instance.requiredXp;
        levelText.text = "Level " + PlayerManager.instance.GetLevel();
        start = false;
    }

    public void UpdateXpUI()
    {
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
                frontXpBarSlider.value = Mathf.Lerp(FXP, backXpBarSlider.value, percentComplete);
            }
        }
        xpText.text = PlayerManager.instance.currentXp + "/" + PlayerManager.instance.requiredXp;
    }

    public void GainExperienceFlatRate(float xpGained)
    {
        PlayerManager.instance.currentXp += xpGained;
        lerptimer = 0f;
        delayTimer = 0f;
    }

    public void GainExperienceScalable(float _xpGained, int _passedLevel)
    {
        if (_passedLevel < PlayerManager.instance.GetLevel())
        {
            float multiplier = 1 + (PlayerManager.instance.GetLevel() - _passedLevel) * 0.1f;
            PlayerManager.instance.currentXp += _xpGained * multiplier;
        }
        else
        {
            PlayerManager.instance.currentXp += _xpGained;
        }
        lerptimer = 0f;
        delayTimer = 0f;
    }

    public void LevelUp()
    {
        PlayerManager.instance.GainLevel();

        PlayerManager.instance.GainSkillsPoints(1);
        PlayerManager.instance.GainStatsPoints(statsPoints); 

        frontXpBarSlider.value = 0;
        backXpBarSlider.value = 0;
        PlayerManager.instance.currentXp = Mathf.RoundToInt(PlayerManager.instance.currentXp - PlayerManager.instance.requiredXp);
        PlayerManager.instance.requiredXp = CalculateRequiredXp();
        levelText.text = "Level " + PlayerManager.instance.GetLevel();

        IncreasedStats();
    }

    private void IncreasedStats()
    {
        CharacterStats stats = PlayerManager.instance.player.stats;

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
        int solveForRequiredXp = 0;
        for (int levelCycle = 1; levelCycle <= PlayerManager.instance.GetLevel(); levelCycle++)
        {
            solveForRequiredXp += (int)Mathf.Floor(levelCycle + additionMultiplier * Mathf.Pow(powerMultiplier, levelCycle / divisionMultiplier));
        }
        return solveForRequiredXp / 4;
    }

    //example of increasing health by level in characterstats
    //public void IncreasedHealth(int level)
    //{
    //    maxHealth += (health * 0.01f) * ((100 - level) * 0.1f);
    //    health = maxHealth;
    //}
}
