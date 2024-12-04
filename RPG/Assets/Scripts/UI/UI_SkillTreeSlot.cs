using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SkillTreeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UI ui;
    [SerializeField] private bool isCheckpoint;

    [SerializeField] private SkillData skillData;
    private string skillName;
    private Image skillImage;
    private int skillCost;
    private string skillDescription;

    [SerializeField] private Color lockedSkillColor;
    public bool unlocked;

    [SerializeField] private UI_SkillTreeSlot[] shouldBeUnlocked;
    [SerializeField] private UI_SkillTreeSlot[] shouldBeLocked;

    private Button button;

    private void OnValidate()
    {
        skillImage = GetComponent<Image>();
        skillImage.sprite = skillData.itemIcon;
        gameObject.name = "SkillTreeSlot_UI - " + skillData.fileName;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => UnlockSkillslot());        
    }

    private void Start()
    {
        skillImage = GetComponent<Image>();

        ui = UIManager.instance.GetUI();

        skillImage.color = lockedSkillColor;

        SetupSkillSlot();

    }

    private void SetupSkillSlot()
    {
        skillName = skillData.skillName;
        skillImage.sprite = skillData.itemIcon;
        skillCost = skillData.skillCost;
        skillDescription = skillData.skillDescription; 
        unlocked = skillData.unlocked;
    }

    private void Update()
    {
        unlocked = skillData.unlocked;
        if (unlocked)
        {
            button.enabled = false;
            skillImage.color = Color.white;
        }
        else
        {
            button.enabled = true;
            skillImage.color = lockedSkillColor;
        }
    }

    public void UnlockSkillslot()
    {
        if(PlayerManager.instance.HaveEnoughSkillsPoints(skillCost) == false)
            return;

        for (int i = 0; i < shouldBeUnlocked.Length; i++)
        {
            if (shouldBeUnlocked[i].unlocked == false)
            {
                Debug.Log("Cannot unlock skill");
                return;
            }
        }

        for (int i = 0; i < shouldBeLocked.Length; i++)
        {
            if (shouldBeLocked[i].unlocked == true)
            {
                Debug.Log("Cannot unlock skill");
                return;
            }
        }
        skillData.unlocked = true;
        unlocked = true;
        skillImage.color = Color.white;
        SkillManager.instance.CheckUnlocks();
    }

    public void ResetSkill()
    {
        PlayerManager.instance.ResetSkillsPoints(skillCost);
        unlocked = false;
        skillData.unlocked = false;
        SkillManager.instance.CheckUnlocks();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isCheckpoint)
        {
            UIManager.instance.GetUICheckpoint().skillToolTip.ShowToolTip(skillDescription, skillName, skillCost);
        }
        else
        {
            ui.skillToolTip.ShowToolTip(skillDescription, skillName, skillCost);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isCheckpoint)
        {
            UIManager.instance.GetUICheckpoint().skillToolTip.HideToolTip();
        }
        else
        {
            ui.skillToolTip.HideToolTip();
        }
    }
}
