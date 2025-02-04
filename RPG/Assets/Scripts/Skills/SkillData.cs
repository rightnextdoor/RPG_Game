using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;


#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SkillType
{
    Blackhole,
    Clone,
    Crystal,
    Dash,
    Dodge,
    Parry,
    Sword,
    Jump
}

[CreateAssetMenu(fileName = "New Skill Data", menuName = "Data/Skills")]
public class SkillData : ScriptableObject
{
    public string skillId;
    public string fileName;
    public SkillType skillType;
    public Sprite itemIcon;
    public string skillName;
    public int skillCost;
    public bool unlocked;
    [TextArea]
    public string skillDescription;

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        skillId = AssetDatabase.AssetPathToGUID(path);
#endif
    }
}
