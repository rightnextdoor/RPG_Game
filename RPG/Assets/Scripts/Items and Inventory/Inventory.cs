using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Inventory : MonoBehaviour, ISaveManager
{
    public static Inventory instance;

    public List<ItemData> startingItems;

    public List<InventoryItem> equipment = new List<InventoryItem>();
    public Dictionary<ItemData_Equipment, InventoryItem> equipmentDictionary = new Dictionary<ItemData_Equipment, InventoryItem>();

    public List<InventoryItem> inventory = new List<InventoryItem>();
    public Dictionary<ItemData, InventoryItem> inventoryDictionary = new Dictionary<ItemData, InventoryItem>();

    public List<InventoryItem> stash = new List<InventoryItem>();
    public Dictionary<ItemData, InventoryItem> stashDictionary = new Dictionary<ItemData, InventoryItem>();

    [Header("Inventory UI")]
    [SerializeField] private Transform inventorySlotParent;
    [SerializeField] private Transform stashSlotParent;
    [SerializeField] private Transform checkpointStashSlotParent;
    [SerializeField] private Transform equpmentSlotParent;
    [SerializeField] private Transform statSlotParent;

    private UI_ItemSlot[] inventoryItemSlots;
    private UI_ItemSlot[] stashItemSlots;
    private UI_ItemSlot[] checkpointStashItemSlots;
    private UI_EquipmentSlot[] equipmentSlots;
    private UI_StatSlot[] statSlot;

    [Header("Items cooldown")]
    private float lastTimeUsedFlask;
    private float lastTimeUsedArmor;

    public float flaskCooldown { get; private set; }
    private float aramorCooldown;

    [Header("Data base")]
    public List<ItemData> itemDataBase;
    public List<InventoryItem> loadedItems = new();
    public List<ItemData_Equipment> loadedEquipment = new();

    private bool hasLoadedItems = false;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        StartCoroutine(InitialGameLoad());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReassignUIOnly());
    }

    private IEnumerator InitialGameLoad()
    {
        yield return new WaitUntil(() =>
            UIManager.instance != null && UIManager.instance.GetUIInventory() != null);

        AssignUIParents();
        AssignUISlots();

        if (!hasLoadedItems)
        {
            AddStartingItems();
            hasLoadedItems = true;
        }
    }

    private IEnumerator ReassignUIOnly()
    {
        yield return new WaitUntil(() =>
            UIManager.instance != null && UIManager.instance.GetUIInventory() != null);

        AssignUIParents();
        AssignUISlots();
        UpdateSlotsUI();
    }

    private void AssignUIParents()
    {
        var ui = UIManager.instance?.GetUIInventory();
        if (ui == null) return;

        inventorySlotParent = ui.inventorySlotParent;
        stashSlotParent = ui.stashSlotParent;
        checkpointStashSlotParent = ui.checkpointStashSlotParent;
        equpmentSlotParent = ui.equipmentSlotParent;
        statSlotParent = ui.statSlotParent;
    }

    private void AssignUISlots()
    {
        inventoryItemSlots = inventorySlotParent.GetComponentsInChildren<UI_ItemSlot>(true);
        stashItemSlots = stashSlotParent.GetComponentsInChildren<UI_ItemSlot>(true);
        checkpointStashItemSlots = checkpointStashSlotParent.GetComponentsInChildren<UI_ItemSlot>(true);
        equipmentSlots = equpmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>(true);
        statSlot = statSlotParent.GetComponentsInChildren<UI_StatSlot>(true);
    }

    private void AddStartingItems()
    {
        foreach (var item in loadedEquipment)
            EquipItem(item);

        if (loadedItems.Count > 0)
        {
            foreach (var item in loadedItems)
                for (int i = 0; i < item.stackSize; i++)
                    AddItem(item.data);
        }
        else
        {
            foreach (var item in startingItems)
                if (item != null)
                    AddItem(item);
        }

    }

    public void EquipItem(ItemData _item)
    {
        ItemData_Equipment newEquipment = _item as ItemData_Equipment;
        InventoryItem newItem = new(newEquipment);

        ItemData_Equipment oldEquipment = null;
        foreach (var item in equipmentDictionary)
        {
            if (item.Key.equipmentType == newEquipment.equipmentType)
                oldEquipment = item.Key;
        }

        if (oldEquipment != null)
        {
            UnequipItem(oldEquipment);
            AddItem(oldEquipment);
        }

        equipment.Add(newItem);
        equipmentDictionary.Add(newEquipment, newItem);
        newEquipment.AddModifiers();

        RemoveItem(_item);
        UpdateSlotsUI();
    }

    public void UnequipItem(ItemData_Equipment itemToRemove)
    {
        if (equipmentDictionary.TryGetValue(itemToRemove, out var value))
        {
            equipment.Remove(value);
            equipmentDictionary.Remove(itemToRemove);
            itemToRemove.RemoveModifiers();
        }
    }

    private void UpdateSlotsUI()
    {
        foreach (var slot in equipmentSlots)
        {
            foreach (var item in equipmentDictionary)
            {
                if (item.Key.equipmentType == slot.slotType)
                    slot.UpdateSlot(item.Value);
            }
        }

        foreach (var slot in inventoryItemSlots)
            slot.CleanUpSlot();
        foreach (var slot in stashItemSlots)
            slot.CleanUpSlot();
        foreach (var slot in checkpointStashItemSlots)
            slot.CleanUpSlot();

        for (int i = 0; i < inventory.Count; i++)
            inventoryItemSlots[i].UpdateSlot(inventory[i]);

        for (int i = 0; i < stash.Count; i++)
        {
            stashItemSlots[i].UpdateSlot(stash[i]);
            checkpointStashItemSlots[i].UpdateSlot(stash[i]);
        }

        UpdateStatsUI();
    }

    public void UpdateStatsUI()
    {
        foreach (var stat in statSlot)
            stat.UpdateStatValueUI();
    }

    public void AddItem(ItemData _item)
    {
        if (_item.itemType == ItemType.Equipment && CanAddItem())
            AddToInventory(_item);
        else if (_item.itemType == ItemType.Material)
            AddToStash(_item);

        UpdateSlotsUI();
    }

    private void AddToStash(ItemData _item)
    {
        if (stashDictionary.TryGetValue(_item, out var value))
            value.AddStack();
        else
        {
            InventoryItem newItem = new(_item);
            stash.Add(newItem);
            stashDictionary.Add(_item, newItem);
        }
    }

    private void AddToInventory(ItemData _item)
    {
        if (inventoryDictionary.TryGetValue(_item, out var value))
            value.AddStack();
        else
        {
            InventoryItem newItem = new(_item);
            inventory.Add(newItem);
            inventoryDictionary.Add(_item, newItem);
        }
    }

    public void RemoveItem(ItemData _item)
    {
        if (inventoryDictionary.TryGetValue(_item, out var value))
        {
            if (value.stackSize <= 1)
            {
                inventory.Remove(value);
                inventoryDictionary.Remove(_item);
            }
            else value.RemoveStack();
        }

        if (stashDictionary.TryGetValue(_item, out var stashValue))
        {
            if (stashValue.stackSize <= 1)
            {
                stash.Remove(stashValue);
                stashDictionary.Remove(_item);
            }
            else stashValue.RemoveStack();
        }

        UpdateSlotsUI();
    }

    public bool CanAddItem()
    {
        if (inventory.Count >= inventoryItemSlots.Length)
        {
            return false;
        }
        return true;
    }

    public bool CanCraft(ItemData_Equipment itemToCraft, List<InventoryItem> required, GameObject ui)
    {
        foreach (var r in required)
        {
            if (!stashDictionary.TryGetValue(r.data, out var stashItem) || stashItem.stackSize < r.stackSize)
                return false;
        }

        foreach (var r in required)
            for (int i = 0; i < r.stackSize; i++)
                RemoveItem(r.data);

        AddItem(itemToCraft);

        if (itemToCraft.equipmentType != EquipmentType.Flask)
        {
            UnlockManager.instance.CraftedItem(itemToCraft);
            ui.GetComponent<UI_CraftList>().CallCraft();
        }

        return true;
    }

    public List<InventoryItem> GetEquipmentList() => equipment;
    public List<InventoryItem> GetStashList() => stash;

    public ItemData_Equipment GetEquipment(EquipmentType type)
    {
        foreach (var item in equipmentDictionary)
            if (item.Key.equipmentType == type)
                return item.Key;
        return null;
    }

    public void UseFlask()
    {
        var flask = GetEquipment(EquipmentType.Flask);
        if (flask == null || Time.time <= lastTimeUsedFlask + flaskCooldown) return;

        AudioManager.instance.PlaySFX("Flask");
        flaskCooldown = flask.itemCooldown;
        flask.Effect(null);
        lastTimeUsedFlask = Time.time;
    }

    public bool CanUseArmor()
    {
        var armor = GetEquipment(EquipmentType.Armor);
        if (armor == null || Time.time <= lastTimeUsedArmor + aramorCooldown) return false;

        aramorCooldown = armor.itemCooldown;
        lastTimeUsedArmor = Time.time;
        return true;
    }

    public void LoadData(GameData _data)
    {
        foreach (var pair in _data.inventory)
        {
            var item = itemDataBase.Find(i => i != null && i.itemId == pair.Key);
            if (item != null)
            {
                InventoryItem loadItem = new(item) { stackSize = pair.Value };
                loadedItems.Add(loadItem);
            }
        }

        foreach (var id in _data.equipmentId)
        {
            var equip = itemDataBase.Find(i => i != null && i.itemId == id) as ItemData_Equipment;
            if (equip != null)
                loadedEquipment.Add(equip);
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.inventory.Clear();
        _data.equipmentId.Clear();

        foreach (var pair in inventoryDictionary)
            _data.inventory[pair.Key.itemId] = pair.Value.stackSize;

        foreach (var pair in stashDictionary)
            _data.inventory[pair.Key.itemId] = pair.Value.stackSize;

        foreach (var pair in equipmentDictionary)
            _data.equipmentId.Add(pair.Key.itemId);
    }

#if UNITY_EDITOR
    [ContextMenu("Fill up item data base")]
    private void FillUpItemDataBase() => itemDataBase = new List<ItemData>(GetItemDataBase());

    private List<ItemData> GetItemDataBase()
    {
        List<ItemData> itemDataBase = new();
        string[] assetName = AssetDatabase.FindAssets("", new[] { "Assets/Data/Items" });

        foreach (string guid in assetName)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var itemData = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (itemData != null)
                itemDataBase.Add(itemData);
        }

        return itemDataBase;
    }
#endif
}
