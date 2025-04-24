using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static Enemy_Boss;

public class UnlockManager : MonoBehaviour, ISaveManager
{
    public static UnlockManager instance;

    [SerializeField] private List<ItemData> lockItemData = new List<ItemData>();
    [SerializeField] private List<ItemData> shopItemData = new List<ItemData>();
    [SerializeField] private List<ItemData_Equipment> craftItemData = new List<ItemData_Equipment>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (transform.root == transform)
            DontDestroyOnLoad(gameObject);
    }

    public List<ItemData> getShopItemData() => shopItemData;
    public List<ItemData_Equipment> getCraftItemData() => craftItemData;

    public void ShopItemEquipment(ItemData_Equipment _shop)
    {
        craftItemData.Add(_shop);
        shopItemData.Remove(_shop);
    }

    public void RemoveMaterial(ItemData _material)
    {
        shopItemData.Remove(_material);
    }

    public void CraftedItem(ItemData_Equipment _crafted)
    {
        if (craftItemData.Contains(_crafted))
        {
            craftItemData.Remove(_crafted);
        }
    }

    public void BossUnlockData(BossType bossType)
    {
        List<ItemData> itemToRemove = new List<ItemData>();

        foreach (ItemData item in lockItemData)
        {
            if (item != null && item.bossUnlockName == bossType)
            {
                shopItemData.Add(item);
                itemToRemove.Add(item);
            }
        }

        foreach (ItemData item in itemToRemove)
        {
            lockItemData.Remove(item);
        }
    }

    public void LevelUnlockData(int _level)
    {
        if (lockItemData.Count == 0)
            return;

        List<ItemData> itemToRemove = new List<ItemData>();

        foreach (ItemData item in lockItemData)
        {
            if (item != null && item.levelUnlock == _level)
            {
                shopItemData.Add(item);
                itemToRemove.Add(item);
            }
        }

        foreach (ItemData item in itemToRemove)
        {
            lockItemData.Remove(item);
        }
    }

    public void LoadData(GameData _data)
    {
        if (_data.lockItemData.Count > 0)
        {
            lockItemData = new List<ItemData>(_data.lockItemData);
        }

        if (_data.shopItemData.Count > 0)
        {
            shopItemData = new List<ItemData>(_data.shopItemData);
        }

        if (_data.craftItemData.Count > 0)
        {
            craftItemData = new List<ItemData_Equipment>(_data.craftItemData);
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.lockItemData.Clear();
        _data.shopItemData.Clear();
        _data.craftItemData.Clear();

        foreach (ItemData item in lockItemData)
        {
            if (item != null)
                _data.lockItemData.Add(item);
        }

        foreach (ItemData item in shopItemData)
        {
            if (item != null)
                _data.shopItemData.Add(item);
        }

        foreach (ItemData_Equipment item in craftItemData)
        {
            if (item != null)
                _data.craftItemData.Add(item);
        }
    }
}
