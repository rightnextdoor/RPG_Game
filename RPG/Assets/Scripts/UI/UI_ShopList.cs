using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_ShopList : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform shopSlotParent;
    [SerializeField] private GameObject shopSlotPrefab;

    [SerializeField] private List<ItemData> shopItem;

    private Color originalColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Image image;

    void Start()
    {
        transform.parent.GetChild(0).GetComponent<UI_ShopList>().SetupShopList();
        SetupDefaultShopWindow();

        image = GetComponent<Image>();
        originalColor = image.color;
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

    public void OnPointerDown(PointerEventData eventData)
    {
        SetupShopList();
    }

    public void SetupDefaultShopWindow()
    {
        if (shopItem[0] != null)
            UIManager.instance.GetUICheckpoint().shopWindow.SetupShopWindow(shopItem[0]);
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
