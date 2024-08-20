using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SkillTreeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UI ui;
    private Image skillImage;

    [SerializeField] private int skillPrice;
    [SerializeField] private string skillName;
    [TextArea]
    [SerializeField] private string skillDescription;
    [SerializeField] private Color lockedSkillColor;

    public bool unlocked;

    [SerializeField] private UI_SkillTreeSlot[] shouldBeUnlocked;
    [SerializeField] private UI_SkillTreeSlot[] shouldBeLocked;


    private void OnValidate()
    {
        gameObject.name = "SkillTreeSlot_UI - " + skillName;
    }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => UnlockSkillslot());        
    }

    private void Start()
    {
        skillImage = GetComponent<Image>();
        ui = GetComponentInParent<UI>();

        skillImage.color = lockedSkillColor;
    }

    public void UnlockSkillslot()
    {
        if(PlayerManager.instance.HaveEnoughMoney(skillPrice) == false)
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
        unlocked = true;
        skillImage.color = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.skillToolTip.ShowToolTip(skillDescription, skillName);

        Vector2 mousePosition = Input.mousePosition;

        float xOffset = 0;
        float yOffset = 0;

        if (mousePosition.x > 1500)
            xOffset = -350;
        else
            xOffset = 350;

        if (mousePosition.y > 930)
            yOffset = -350;
        else
            yOffset = 350;

        ui.skillToolTip.transform.position = new Vector2(mousePosition.x + xOffset, mousePosition.y + yOffset);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.skillToolTip.HideToolTip();
    }
}
