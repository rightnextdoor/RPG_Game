using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSystem : MonoBehaviour
{
    //[SerializeField] private PlayerStats playerStats;
    private float lerptimer;
    private float delayTimer = 0;
    public int delaySpeed = 4;
    [SerializeField] private int skillsPoints = 5;
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
        levelText.text = "Level " + PlayerManager.instance.level;
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
        if (PlayerManager.instance.level == 0)
        {
            PlayerManager.instance.level = 1;
            PlayerManager.instance.requiredXp = CalculateRequiredXp();
        }
        frontXpBarSlider.value = PlayerManager.instance.currentXp / PlayerManager.instance.requiredXp;
        backXpBarSlider.value = PlayerManager.instance.currentXp / PlayerManager.instance.requiredXp;
        levelText.text = "Level " + PlayerManager.instance.level;
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
        if (_passedLevel < PlayerManager.instance.level)
        {
            float multiplier = 1 + (PlayerManager.instance.level - _passedLevel) * 0.1f;
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
        PlayerManager.instance.level++;
        PlayerManager.instance.skillsPoint++;
        PlayerManager.instance.statsPoints += skillsPoints;

        if(GetComponent<UI_Stats>() != null)
            GetComponent<UI_Stats>().UpdateStartingPoints(skillsPoints);

        frontXpBarSlider.value = 0;
        backXpBarSlider.value = 0;
        PlayerManager.instance.currentXp = Mathf.RoundToInt(PlayerManager.instance.currentXp - PlayerManager.instance.requiredXp);
        //gain skills points and stats
        PlayerManager.instance.requiredXp = CalculateRequiredXp();
        levelText.text = "Level " + PlayerManager.instance.level;
    }

    private int CalculateRequiredXp()
    {
        int solveForRequiredXp = 0;
        for (int levelCycle = 1; levelCycle <= PlayerManager.instance.level; levelCycle++)
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
