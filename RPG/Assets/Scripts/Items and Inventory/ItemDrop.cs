using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [SerializeField] private int maxItemsToDrop;
    [SerializeField] private ItemData[] itemPool;
    [SerializeField]private List<ItemData> possibleDrop = new List<ItemData>();

    [SerializeField] private GameObject dropPrefab;

    public virtual void GenerateDrop()
    {
        if (itemPool.Length == 0)
        {
            Debug.Log("Item Pool is empty. Enemy cannot drop items.");
            return;
        }

        foreach (ItemData item in itemPool)
        {
            if (item != null && Random.Range(0, 100) < item.dropChance)
            {
                if(item.itemType == ItemType.Material)
                    possibleDrop.Add(item);
            }
        }

        for (int i = 0; i < maxItemsToDrop; i++)
        {
            if (possibleDrop.Count > 0)
            {
                ItemData itemToDrop = possibleDrop[Random.Range(0, possibleDrop.Count)];
                DropItem(itemToDrop);
                possibleDrop.Remove(itemToDrop);
            }
        }
    }

    protected void DropItem(ItemData _itemData)
    {
        GameObject newDrop = Instantiate(dropPrefab, transform.position, Quaternion.identity);

        //Vector2 randomVelocity = new Vector2(Random.Range(-5, 5), Random.Range(15, 20)); // To make the enemy drop an Item on the ground
        //newDrop.GetComponent<ItemObject>().SetupItem(_itemData, randomVelocity);

        newDrop.GetComponent<ItemObject>().AddItemToInventory(_itemData);
    }
}
