using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private ItemData itemData;

    private void OnValidate()
    {
        GetComponent<SpriteRenderer>().sprite = itemData.itemIcon;
        gameObject.name = "Item object - " + itemData.itemName;
    }

    private void SetupVisuals()
    {
        if (itemData == null)
            return;

        GetComponent<SpriteRenderer>().sprite = itemData.itemIcon;
        gameObject.name = "Item object - " + itemData.name;
    }

    public void SetupItem(ItemData _itemData, Vector2 _velocity)
    {
        itemData = _itemData;
        rb.velocity = _velocity;

        SetupVisuals();
    }

    public void PickupItem()
    {
        if (!Inventory.instance.CanAddItem() && itemData.itemType == ItemType.Equipment)
        {
            rb.velocity = new Vector2(0, 7);
            NotificationManager.instance.SetNewNotification("Inventory is full");
            return;
        }

        AudioManager.instance.PlaySFX("Item", transform);
        Inventory.instance.AddItem(itemData);
        Destroy(gameObject);
        NotificationManager.instance.SetNewNotification(itemData.name + " Added");
    }

    public void AddItemToInventory(ItemData _itemData)
    {
        if (!Inventory.instance.CanAddItem() && _itemData.itemType == ItemType.Equipment)
        {
            NotificationManager.instance.SetNewNotification("Inventory is full");
            return;
        }

        AudioManager.instance.PlaySFX("Item", transform);
        Inventory.instance.AddItem(_itemData);

        NotificationManager.instance.SetNewNotification(_itemData.name + " Added");
        Destroy(gameObject);
    }
}
