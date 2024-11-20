using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerManager : MonoBehaviour, ISaveManager
{
    public static PlayerManager instance;
    public Player player;

    [SerializeField] private int currency = 0;
    private int maxCurrency = 10000000;
    [SerializeField] private TextMeshProUGUI currencyText;

    public int skillsPoint = 0;
    public int statsPoints = 0;
    public int level = 1;
    public float currentXp = 0;
    public float requiredXp = 0;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;

    }

    private void Update()
    {
        currencyText.text = currency.ToString();
    }

    public void GainXP(float _xpGained, int _passedLevel)
    {
        GetComponent<LevelSystem>().GainExperienceScalable(_xpGained, _passedLevel);
    }

    public void GainCurrency(int _currency)
    {
        if (currency + _currency >= maxCurrency)
            return;

        currency += _currency;

        if(currency > maxCurrency)
            currency = maxCurrency;
    }

    public int GetCurrecncy() { return currency; }

    public bool HaveEnoughGold(int _price)
    {
        if (_price > currency)
        {
            Debug.Log("Not enough skills gold");
            return false;
        }

        currency = currency - _price;
        return true;
    }

    public bool HaveEnoughSkillsPoints(int _price)
    {
        if (_price > skillsPoint)
        {
            Debug.Log("Not enough skills points");
            return false;
        }

        skillsPoint = skillsPoint - _price;
        return true;
    }

    public bool HaveEnoughStatsPoints(int _price)
    {
        if (_price > statsPoints)
        {
            Debug.Log("Not enough stats points");
            return false;
        }

        statsPoints = statsPoints - _price;
        return true;
    }

    public int GetSkillsPoints() => skillsPoint;
    public int GetStatsPoints() => statsPoints;

    public void LoadData(GameData _data)
    {
        this.skillsPoint = _data.skillsPoints;
        this.statsPoints = _data.statsPoints;
        this.level = _data.level;
        this.currentXp = _data.currentXp;
        this.requiredXp = _data.requiredXp;

        this.currency = _data.currency;

    }

    public void SaveData(ref GameData _data)
    {
        _data.skillsPoints = this.skillsPoint;
        _data.statsPoints = this.statsPoints;
        _data.level = this.level;
        _data.currentXp = this.currentXp;
        _data.requiredXp = this.requiredXp;

        _data.currency = this.currency;
    }
}
