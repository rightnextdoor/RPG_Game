using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

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

    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject armor;
    [SerializeField] private GameObject amulet;
    [SerializeField] private GameObject flask;
    [SerializeField] private GameObject material;

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
        if (playerManager.HaveEnoughGold(itemData.itemCost))
        {
            if (itemData.itemType == ItemType.Equipment)
            {
                UnlockManager.instance.ShopItemEquipment(itemData as ItemData_Equipment);
                ItemData_Equipment equipment = itemData as ItemData_Equipment;
                if (equipment.equipmentType == EquipmentType.Weapon)
                    weapon.GetComponent<UI_ShopList>().CallShop();
                else if(equipment.equipmentType == EquipmentType.Armor)
                    armor.GetComponent<UI_ShopList>().CallShop();
                else if (equipment.equipmentType == EquipmentType.Amulet)
                    amulet.GetComponent<UI_ShopList>().CallShop();
                else if (equipment.equipmentType == EquipmentType.Flask)
                    flask.GetComponent<UI_ShopList>().CallShop();

            } else
            {
                if (itemData.itemType == ItemType.Material)
                {
                    Inventory.instance.AddItem(itemData);
                    UnlockManager.instance.RemoveMaterial(itemData);
                    material.GetComponent<UI_ShopList>().CallShop();
                }
            }

            
        } else
        {
            NotificationManager.instance.SetNewNotification("Not enough money to buy!");
            Debug.Log("Not enough money to buy");
        }
    }
}
