using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CraftWindow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Button craftButton;

    [SerializeField] private Image[] materialImage;
    private ItemData_Equipment itemData;

    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject armor;
    [SerializeField] private GameObject amulet;

    public void SetupCraftWindow(ItemData_Equipment _data)
    {
        craftButton.onClick.RemoveAllListeners();

        itemData = _data;

        for (int i = 0; i < materialImage.Length; i++)
        {
            materialImage[i].color = Color.clear;
            materialImage[i].GetComponentInChildren<TextMeshProUGUI>().color = Color.clear;
        }

        for (int i = 0; i < _data.craftingMaterials.Count; i++)
        {
            if (_data.craftingMaterials.Count > materialImage.Length)
                Debug.LogWarning("You have more materials amount than you have material slots in craft window");

            materialImage[i].sprite = _data.craftingMaterials[i].data.itemIcon;
            materialImage[i].color = Color.white;

            TextMeshProUGUI materialSlotText = materialImage[i].GetComponentInChildren<TextMeshProUGUI>();

            materialSlotText.text = _data.craftingMaterials[i].stackSize.ToString();
            materialSlotText.color = Color.white;
        }

        itemIcon.sprite = _data.itemIcon;
        itemName.text = _data.itemName;
        itemDescription.text = _data.GetDescription();

        craftButton.onClick.AddListener(() => CraftItem());
    }

    private void CraftItem()
    {
        ItemData_Equipment equipment = itemData;
        GameObject passInGameObject = null;

        if (equipment.equipmentType == EquipmentType.Weapon)
            passInGameObject = weapon;
        else if (equipment.equipmentType == EquipmentType.Armor)
            passInGameObject = armor;
        else if (equipment.equipmentType == EquipmentType.Amulet)
            passInGameObject = amulet;

        Inventory.instance.CanCraft(itemData, itemData.craftingMaterials,passInGameObject);
    }
}
