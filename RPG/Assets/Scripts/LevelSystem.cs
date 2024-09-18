using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSystem : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    public int level;
    public float currentXp;
    public float requiredXp;

    private float lerptimer;
    private float delayTimer = 0;
    public int delaySpeed = 4;
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
    
 
    void Start()
    {
        frontXpBarSlider.value = currentXp / requiredXp;
        backXpBarSlider.value = currentXp / requiredXp;
        requiredXp = CalculateRequiredXp();
        levelText.text = "Level " + level;
    }


    void Update()
    {
        UpdateXpUI();

        if (Input.GetKeyDown(KeyCode.Y))
        {
            GainExperienceFlatRate(20);
        }

        if (currentXp > requiredXp)
        {
            LevelUp();
        }
    }

    public void UpdateXpUI()
    {
        float xpFraction = currentXp / requiredXp;
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
        xpText.text = currentXp + "/" + requiredXp;
    }

    public void GainExperienceFlatRate(float xpGained)
    {
        currentXp += xpGained;
        lerptimer = 0f;
        delayTimer = 0f;
    }

    public void GainExperienceScalable(float _xpGained, int _passedLevel)
    {
        if (_passedLevel < level)
        {
            float multiplier = 1 + (level - _passedLevel) * 0.1f;
            currentXp += _xpGained * multiplier;
        }
        else
        {
            currentXp += _xpGained;
        }
        lerptimer = 0f;
        delayTimer = 0f;
    }

    public void LevelUp()
    {
        level++;
        frontXpBarSlider.value = 0;
        backXpBarSlider.value = 0;
        currentXp = Mathf.RoundToInt(currentXp - requiredXp);
        //gain skills points and stats
        requiredXp = CalculateRequiredXp();
        levelText.text = "Level " + level;
    }

    private int CalculateRequiredXp()
    {
        int solveForRequiredXp = 0;
        for (int levelCycle = 1; levelCycle <= level; levelCycle++)
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
