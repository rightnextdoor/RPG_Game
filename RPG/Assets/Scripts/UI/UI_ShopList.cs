using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_ShopList : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform shopSlotParent;
    [SerializeField] private GameObject shopSlotPrefab;
    [SerializeField] private ItemType itemType;
    [SerializeField] private EquipmentType equipmentType;
    
    [SerializeField] private List<ItemData> shopItem = new List<ItemData>();

    private Color originalColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Image image;

    [SerializeField] private GameObject shopWhindow;

    private void Start()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    public void CallShop()
    {
        List<ItemData> itemData = UnlockManager.instance.getShopItemData();
        shopItem.Clear();
        foreach (ItemData item in itemData)
        {
            if (itemType == ItemType.Equipment)
            {
                ItemData_Equipment equipment = item as ItemData_Equipment;
                if (equipment != null)
                {
                    if (equipment.equipmentType == equipmentType)
                    {
                        shopItem.Add(item);
                    }
                }          
            } else
            {
                if (itemType == item.itemType)
                {
                    shopItem.Add(item);
                }        
            }
        }
        if (shopItem.Count != 0)
        {
            SetupShopList();
            SetupDefaultShopWindow();
        } else
        {
            for (int i = 0; i < shopSlotParent.childCount; i++)
            {
                Destroy(shopSlotParent.GetChild(i).gameObject);
            }
            shopWhindow.gameObject.SetActive(false);
        }
        
    }


    public void SetupShopList()
    {
        for (int i = 0; i < shopSlotParent.childCount; i++)
        {
            Destroy(shopSlotParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < shopItem.Count; i++)
        {
            GameObject newSlot = Instantiate(shopSlotPrefab, shopSlotParent);
            newSlot.GetComponent<UI_ShopSlot>().SetupShopSlot(shopItem[i]);
        }
    }
    private void SetupDefaultShopWindow()
    {
        if(!shopWhindow.gameObject.activeSelf)
            shopWhindow.gameObject.SetActive(true);

        if (shopItem[0] != null)
            UIManager.instance.GetUICheckpoint().shopWindow.SetupShopWindow(shopItem[0]);
            
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CallShop();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = originalColor;
    }
}
