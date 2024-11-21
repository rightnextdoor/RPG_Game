using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_CraftSlot : UI_ItemSlot
{
    private bool isCheckpoint;
    private Color originalColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Image image;
    protected override void Start()
    {
        base.Start();
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    public void SetupCraftSlot(ItemData_Equipment _data, bool _isCheckpoint)
    {
        if (_data == null)
            return;
        
        isCheckpoint = _isCheckpoint;

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
        if (isCheckpoint)
        {
            UIManager.instance.GetUICheckpoint().craftWindow.SetupCraftWindow(item.data as ItemData_Equipment);
        } else
        {
            UIManager.instance.GetUI().craftWindow.SetupCraftWindow(item.data as ItemData_Equipment);
        }
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
