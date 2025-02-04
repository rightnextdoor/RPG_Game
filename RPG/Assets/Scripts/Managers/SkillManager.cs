using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkillManager : MonoBehaviour, ISaveManager
{
    public static SkillManager instance;

    [Header("Data base")]
    public List<SkillData> skillDataBase;

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
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
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

        Invoke("CheckUnlocks", .2f);
    }

    public void CheckUnlocks()
    {
        List<SkillData> swordData = new List<SkillData>();
        List<SkillData> dashData = new List<SkillData>();
        List<SkillData> cloneData = new List<SkillData>();
        List<SkillData> blackholeData = new List<SkillData>();
        List<SkillData> crystalData = new List<SkillData>();
        List<SkillData> parryData = new List<SkillData>();
        List<SkillData> dodgeData = new List<SkillData>();
        List<SkillData> jumpData = new List<SkillData>();
        
        SetupList(swordData, dashData, cloneData, blackholeData, crystalData, parryData, dodgeData, jumpData);

        dash.CheckUnlock(dashData);
        clone.CheckUnlock(cloneData);
        sword.CheckUnlock(swordData);
        blackhole.CheckUnlock(blackholeData);
        crystal.CheckUnlock(crystalData);
        parry.CheckUnlock(parryData);
        dodge.CheckUnlock(dodgeData);
        doubleJump.CheckUnlock(jumpData);
        
    }

    private void SetupList(List<SkillData> swordData, List<SkillData> dashData, List<SkillData> cloneData, List<SkillData> blackholeData, List<SkillData> crystalData, List<SkillData> parryData, List<SkillData> dodgeData, List<SkillData> jumpData)
    {
        foreach (SkillData skillData in skillDataBase)
        {
            if (SkillType.Sword == skillData.skillType)
            {
                swordData.Add(skillData);
            }
            if (SkillType.Dash == skillData.skillType)
            {
                dashData.Add(skillData);
            }
            if (SkillType.Clone == skillData.skillType)
            {
                cloneData.Add(skillData);
            }
            if (SkillType.Blackhole == skillData.skillType)
            {
                blackholeData.Add(skillData);
            }
            if (SkillType.Crystal == skillData.skillType)
            {
                crystalData.Add(skillData);
            }
            if (SkillType.Parry == skillData.skillType)
            {
                parryData.Add(skillData);
            }
            if (SkillType.Dodge == skillData.skillType)
            {
                dodgeData.Add(skillData);
            }
            if (SkillType.Jump == skillData.skillType)
            {
                jumpData.Add(skillData);
            }
        }
    }

    public void LockSkills()
    {
        foreach (var item in skillDataBase)
        {
            item.unlocked = false;
        }
    }

    public void LoadData(GameData _data)
    {
        if (_data.skillTree.Count == 0)
        {
            LockSkills();
        }
        foreach (KeyValuePair<string, bool> pair in _data.skillTree)
        {
            foreach (var item in skillDataBase)
            {
                if (item != null && item.skillId == pair.Key)
                {
                    item.unlocked = pair.Value;
                }
            }
        }

    }

    public void SaveData(ref GameData _data)
    {
        _data.skillTree.Clear();

        foreach (SkillData pair in skillDataBase)
        {
            _data.skillTree.Add(pair.skillId, pair.unlocked);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Fill up skill data base")]
    private void FillUpItemDataBase() => skillDataBase = new List<SkillData>(GetItemDataBase());

    private List<SkillData> GetItemDataBase()
    {
        List<SkillData> skillDataBase = new List<SkillData>();
        string[] assetName = AssetDatabase.FindAssets("", new[] { "Assets/Data/Skills" });

        foreach (string SOName in assetName)
        {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<SkillData>(SOpath);
            skillDataBase.Add(itemData);
        }

        return skillDataBase;
    }
#endif
}
