using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour, ISaveManager
{
    public static PlayerManager instance;

    [Header("Player Reference")]
    private Player _player;
    public Player player
    {
        get
        {
            if (_player == null)
            {
                TryRebindPlayer();
                if (_player == null)
                {
                    Debug.LogWarning("[PlayerManager] Player reference is null.");
                }
            }

            return _player;
        }
        set => _player = value;
    }

    [Header("Currency")]
    [SerializeField] private int currency = 0;
    private int maxCurrency = 10000000;
    private TextMeshProUGUI currencyText;

    [Header("Stats")]
    private int skillsPoint = 0;
    private int statsPoints = 0;
    private int totalStatsPoints = 0;
    private int level = 1;
    public float currentXp = 0;
    public float requiredXp = 0;

    private bool levelTransition;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void TryRebindPlayer()
    {
        Player found = GameObject.FindObjectOfType<Player>();
        if (found != null)
        {
            AssignPlayer(found);
        }
    }


    private void OnEnable()
    {
        Player.OnPlayerSpawned += AssignPlayer;
        StartCoroutine(WaitForUIAndAssignText());
    }

    private void OnDisable()
    {
        Player.OnPlayerSpawned -= AssignPlayer;
    }

    private IEnumerator WaitForUIAndAssignText()
    {
        yield return new WaitUntil(() =>
            UIManager.instance != null && UIManager.instance.GetUIPlayer() != null);

        currencyText = UIManager.instance.GetUIPlayer().currencyText;
    }

    private void AssignPlayer(Player spawnedPlayer)
    {
        player = spawnedPlayer;
    }

    private void Update()
    {
        if (currencyText != null)
            currencyText.text = currency.ToString();
    }

    #region Currency

    public int GetCurrecncy() => currency;

    public void GainCurrency(int _currency)
    {
        if (currency + _currency >= maxCurrency)
            return;

        currency += _currency;
        if (currency > maxCurrency)
            currency = maxCurrency;
    }

    public bool HaveEnoughGold(int _price)
    {
        if (_price > currency)
        {
            Debug.Log("Not enough gold");
            return false;
        }

        currency -= _price;
        return true;
    }

    #endregion

    #region Points & XP

    public void GainXP(float _xpGained, int _passedLevel)
    {
        GetComponent<LevelSystem>().GainExperienceScalable(_xpGained, _passedLevel);
    }

    public void GainLevel()
    {
        level++;
        UnlockManager.instance.LevelUnlockData(level);
    }

    public int GetLevel() => level;
    public int GetSkillsPoints() => skillsPoint;
    public int GetStatsPoints() => statsPoints;
    public int GetTotalStatsPoints() => totalStatsPoints;

    public void GainSkillsPoints(int _points) => skillsPoint += _points;
    public void GainStatsPoints(int _points)
    {
        statsPoints += _points;
        totalStatsPoints += _points;
    }

    public void ResetSkillsPoints(int _points) => skillsPoint += _points;
    public void ResetStatsPoints() => statsPoints = totalStatsPoints;

    public bool HaveEnoughSkillsPoints(int _price)
    {
        if (_price > skillsPoint)
        {
            Debug.Log("Not enough skill points");
            return false;
        }

        skillsPoint -= _price;
        return true;
    }

    public bool HaveEnoughStatsPoints(int _price)
    {
        if (_price > statsPoints)
        {
            Debug.Log("Not enough stat points");
            return false;
        }

        statsPoints -= _price;
        return true;
    }

    #endregion

    #region Transition

    public bool IsLevelTranstion() => levelTransition;
    public void LevelTranstion(bool isTranstion) => levelTransition = isTranstion;

    #endregion

    #region Save/Load

    public void LoadData(GameData _data)
    {
        skillsPoint = _data.skillsPoints;
        statsPoints = _data.statsPoints;
        totalStatsPoints = _data.totalStatsPoints;
        level = _data.level;
        currentXp = _data.currentXp;
        requiredXp = _data.requiredXp;
        currency = _data.currency;
    }

    public void SaveData(ref GameData _data)
    {
        _data.skillsPoints = skillsPoint;
        _data.statsPoints = statsPoints;
        _data.totalStatsPoints = totalStatsPoints;
        _data.level = level;
        _data.currentXp = currentXp;
        _data.requiredXp = requiredXp;
        _data.currency = currency;
    }


    #endregion
}
