using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_ShopSlot : UI_ItemSlot
{
    private Color originalColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Image image;

    protected override void Start()
    {
        base.Start();
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    public void SetupShopSlot(ItemData _data)
    {
        if (_data == null)
            return;

        item.data = _data;

        itemImage.sprite = _data.itemIcon;
        itemText.text = _data.itemName;

        if (itemText.text.Length > 12)
            itemText.fontSize = itemText.fontSize * .7f;
        else
            itemText.fontSize = 24;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        UIManager.instance.GetUICheckpoint().shopWindow.SetupShopWindow(item.data);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        image.color = highlightColor;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        image.color = originalColor;
    }
}
