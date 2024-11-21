using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_CraftList : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform craftSlotParent;
    [SerializeField] private GameObject craftSlotPrefab;

    [SerializeField] private List<ItemData_Equipment> craftEquipment;
    [SerializeField] private bool isCheckpoint;

    private Color originalColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Image image;

    void Start()
    {
        transform.parent.GetChild(0).GetComponent<UI_CraftList>().SetupCraftList();
        SetupDefaultCraftWindow();

        image = GetComponent<Image>();
        originalColor = image.color;
    }


    public void SetupCraftList()
    {
        for (int i = 0; i < craftSlotParent.childCount; i++)
        {
            Destroy(craftSlotParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < craftEquipment.Count; i++)
        {
            GameObject newSlot = Instantiate(craftSlotPrefab, craftSlotParent);
            newSlot.GetComponent<UI_CraftSlot>().SetupCraftSlot(craftEquipment[i], isCheckpoint);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetupCraftList();
    }

    public void SetupDefaultCraftWindow()
    {      
        if (craftEquipment[0] != null)
        {
            if (isCheckpoint)
            {
                UIManager.instance.GetUICheckpoint().craftWindow.SetupCraftWindow(craftEquipment[0]);
            }
            else
            {           
                UIManager.instance.GetUI().craftWindow.SetupCraftWindow(craftEquipment[0]);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = originalColor;
    }
}
