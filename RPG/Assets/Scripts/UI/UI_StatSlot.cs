using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_StatSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UI ui;

    [SerializeField] private StatsData statData;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private TextMeshProUGUI statNameText;

    private void OnValidate()
    {
        gameObject.name = "Stat - " + statData.statName;

        if(statNameText != null )
            statNameText.text = statData.statName;
    }

    void Start()
    {
        UpdateStatValueUI();

        ui = UIManager.instance.GetUI();
    }

    public void UpdateStatValueUI()
    {
        PlayerStats playerStats = PlayerManager.instance.player.gameObject.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            statValueText.text = playerStats.GetStat(statData.statType).GetValue().ToString();

            if(statData.statType == StatType.health)
                statValueText.text = playerStats.GetMaxHealthValue().ToString();

            if (statData.statType == StatType.damage)
                statValueText.text = (playerStats.damage.GetValue() + playerStats.strength.GetValue()).ToString();

            if (statData.statType == StatType.critPower)
                statValueText.text = (playerStats.critPower.GetValue() + playerStats.strength.GetValue()).ToString();

            if (statData.statType == StatType.critChance)
                statValueText.text = (playerStats.critChance.GetValue() + playerStats.agility.GetValue()).ToString();

            if (statData.statType == StatType.evasion)
                statValueText.text = (playerStats.evasion.GetValue() + playerStats.agility.GetValue()).ToString();

            if (statData.statType == StatType.magicResistance)
                statValueText.text = (playerStats.magicResistance.GetValue() + playerStats.intelligence.GetValue() * 3).ToString();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.statToolTip.ShowStatToolTip(statData.statsDescription);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.statToolTip.HideStatToolTip();
    }
}
