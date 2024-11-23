using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillReset : MonoBehaviour
{
    [SerializeField] private Transform skillTreeParent;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI skillsPointText;

    private void Awake()
    {
        resetButton.onClick.RemoveAllListeners();

        resetButton.onClick.AddListener(() => ResetSkills());
    }

    private void Update()
    {
        skillsPointText.text = PlayerManager.instance.skillsPoint.ToString();
    }

    private void ResetSkills()
    {
        for (int i = 0; i < skillTreeParent.childCount; i++)
        {
            if (skillTreeParent.GetChild(i).GetComponentsInChildren<UI_SkillTreeSlot>() != null)
            {
                UI_SkillTreeSlot[] child = skillTreeParent.GetChild(i).GetComponentsInChildren<UI_SkillTreeSlot>();
                
                for (int j = 0; j < child.Length; j++)
                {
                    if (child[j].unlocked)
                    {                       
                        child[j].ResetSkill();                       
                    }
                }
                
            }
        }
    }

}
