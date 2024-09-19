using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerManager : MonoBehaviour, ISaveManager
{
    public static PlayerManager instance;
    public Player player;

    public int skillsPoint = 0;
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

    public void GainXP(float _xpGained, int _passedLevel)
    {
        GetComponent<LevelSystem>().GainExperienceScalable(_xpGained, _passedLevel);
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

    public int GetSkillsPoints() => skillsPoint;

    public void LoadData(GameData _data)
    {
        this.skillsPoint = _data.skillsPoints;
        this.level = _data.level;
        this.currentXp = _data.currentXp;
        this.requiredXp = _data.requiredXp;

    }

    public void SaveData(ref GameData _data)
    {
        _data.skillsPoints = this.skillsPoint;
        _data.level = this.level;
        _data.currentXp = this.currentXp;
        _data.requiredXp = this.requiredXp;
    }
}
