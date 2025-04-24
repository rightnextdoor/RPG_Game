using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] protected Image itemImage;
    [SerializeField] protected TextMeshProUGUI itemText;

    public InventoryItem item;

    protected virtual void Start()
    {
    }

    public void UpdateSlot(InventoryItem _newItem)
    {
        item = _newItem;

        itemImage.color = Color.white;

        if (item != null)
        {
            itemImage.sprite = item.data.itemIcon;

            if (item.stackSize > 1)
            {
                itemText.text = item.stackSize.ToString();
            }
            else
            {
                itemText.text = "";
            }
        }
    }

    public void CleanUpSlot()
    {
        item = null;

        itemImage.sprite = null;
        itemImage.color = Color.clear;
        itemText.text = "";
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (item == null)
            return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            Inventory.instance.RemoveItem(item.data);
            return;
        }
        Debug.Log("pointer down item " + item.data);
        if (item.data.itemType == ItemType.Equipment)
            Inventory.instance.EquipItem(item.data);

        ToolTipManager.instance.itemToolTip.HideItemTooltip();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if(item == null) 
            return;

        ToolTipManager.instance.itemToolTip.ShowItemTooltip(item.data as ItemData_Equipment);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (item == null)
            return;

        ToolTipManager.instance.itemToolTip.HideItemTooltip();
    }
}
