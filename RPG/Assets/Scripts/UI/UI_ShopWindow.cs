using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ShopWindow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI itemCost;
    [SerializeField] private TextMeshProUGUI currency;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Button shopButton;
    PlayerManager playerManager;
    private ItemData itemData;

    private void Update()
    {
        currency.text = "$ " + playerManager.GetCurrecncy();
    }
    public void SetupShopWindow(ItemData _data)
    {
        shopButton.onClick.RemoveAllListeners();

        playerManager = PlayerManager.instance;
        itemData = _data;

        itemIcon.sprite = _data.itemIcon;
        itemName.text = _data.itemName;
        itemDescription.text = _data.GetDescription();
        itemCost.text = "$ " + _data.itemCost;      

        shopButton.onClick.AddListener(() => AddToItemCraft());
    }

    private void AddToItemCraft()
    {
        //temp until make item unlock system
        if (playerManager.HaveEnoughGold(itemData.itemCost))
        {
            Debug.Log("Item " + itemData.itemName + " add to unlock items");
        } else
        {
            Debug.Log("Not enough money to buy");
        }
    }
}
