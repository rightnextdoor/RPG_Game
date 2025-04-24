using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkillManager : MonoBehaviour, ISaveManager
{
    public static SkillManager instance;

    [Header("Skill Database")]
    [SerializeField] private List<SkillData> skillDataBase = new List<SkillData>();

    [Header("Runtime Flags")]
    public bool inBlackholeState;

    public Dash_Skill dash { get; private set; }
    public Clone_Skill clone { get; private set; }
    public Sword_Skill sword { get; private set; }
    public Blackhole_Skill blackhole { get; private set; }
    public Crystal_Skill crystal { get; private set; }
    public Parry_Skill parry { get; private set; }
    public Dodge_Skill dodge { get; private set; }
    public DoubleJump_Skill doubleJump { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Automatically reparent under GameManager if not already
        if (transform.parent == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm != null) transform.SetParent(gm.transform);
        }
    }


    private void Start()
    {
        dash = GetComponent<Dash_Skill>();
        clone = GetComponent<Clone_Skill>();
        sword = GetComponent<Sword_Skill>();
        blackhole = GetComponent<Blackhole_Skill>();
        crystal = GetComponent<Crystal_Skill>();
        parry = GetComponent<Parry_Skill>();
        dodge = GetComponent<Dodge_Skill>();
        doubleJump = GetComponent<DoubleJump_Skill>();

        Invoke(nameof(CheckUnlocks), 0.2f);
    }

    public void CheckUnlocks()
    {
        List<SkillData> swordData = new();
        List<SkillData> dashData = new();
        List<SkillData> cloneData = new();
        List<SkillData> blackholeData = new();
        List<SkillData> crystalData = new();
        List<SkillData> parryData = new();
        List<SkillData> dodgeData = new();
        List<SkillData> jumpData = new();

        foreach (SkillData data in skillDataBase)
        {
            if (data == null) continue;

            switch (data.skillType)
            {
                case SkillType.Sword: swordData.Add(data); break;
                case SkillType.Dash: dashData.Add(data); break;
                case SkillType.Clone: cloneData.Add(data); break;
                case SkillType.Blackhole: blackholeData.Add(data); break;
                case SkillType.Crystal: crystalData.Add(data); break;
                case SkillType.Parry: parryData.Add(data); break;
                case SkillType.Dodge: dodgeData.Add(data); break;
                case SkillType.Jump: jumpData.Add(data); break;
            }
        }

        dash?.CheckUnlock(dashData);
        clone?.CheckUnlock(cloneData);
        sword?.CheckUnlock(swordData);
        blackhole?.CheckUnlock(blackholeData);
        crystal?.CheckUnlock(crystalData);
        parry?.CheckUnlock(parryData);
        dodge?.CheckUnlock(dodgeData);
        doubleJump?.CheckUnlock(jumpData);
    }

    public void LockSkills()
    {
        foreach (var item in skillDataBase)
        {
            if (item != null)
                item.unlocked = false;
        }
    }

    public void LoadData(GameData _data)
    {
        if (_data.skillTree.Count == 0)
        {
            LockSkills();
            return;
        }

        foreach (var item in skillDataBase)
        {
            if (item != null && _data.skillTree.TryGetValue(item.skillId, out bool isUnlocked))
            {
                item.unlocked = isUnlocked;
            }
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.skillTree.Clear();

        foreach (SkillData skill in skillDataBase)
        {
            if (skill != null)
                _data.skillTree.Add(skill.skillId, skill.unlocked);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Fill up skill database")]
    private void FillUpItemDataBase()
    {
        skillDataBase = GetItemDataBase().FindAll(s => s != null);
    }

    private List<SkillData> GetItemDataBase()
    {
        List<SkillData> dataBase = new();
        string[] assetGUIDs = AssetDatabase.FindAssets("", new[] { "Assets/Data/Skills" });

        foreach (string guid in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null)
                dataBase.Add(skill);
        }

        return dataBase;
    }
#endif
}
