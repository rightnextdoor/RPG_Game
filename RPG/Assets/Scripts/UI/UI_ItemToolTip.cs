using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UI_ItemToolTip : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private GameObject background;
    [SerializeField] private TextMeshProUGUI descriptionText;
    private RectTransform backgroundRectTransform;
    private RectTransform rectTransform;

    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private Image icon;

    private void Awake()
    {

        backgroundRectTransform = background.GetComponent<RectTransform>();
        rectTransform = transform.GetComponent<RectTransform>();

        HideTooltip();

    }

    private void Update()
    {
        Vector2 anchoredPosition = Input.mousePosition / canvasRectTransform.localScale.x;

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, 0, canvasRectTransform.rect.width - backgroundRectTransform.rect.width);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0, canvasRectTransform.rect.height - backgroundRectTransform.rect.height);

        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    private void SetText(string tooltipString)
    {
        descriptionText.SetText(tooltipString);
        descriptionText.ForceMeshUpdate();

        //Vector2 textSize = descriptionText.GetRenderedValues(false);
        //Vector2 paddingSize = descriptionText.margin * 2;
        //backgroundRectTransform.localScale = textSize + paddingSize;
    }

    public void ShowItemTooltip(ItemData_Equipment item)
    {
        if(item == null) 
            return;

        gameObject.SetActive(true);

        itemNameText.SetText(item.itemName);
        itemTypeText.SetText(item.equipmentType.ToString());
        icon.sprite = item.itemIcon;
        SetText(item.GetDescription());
    }

    public void HideItemTooltip()
    {
        gameObject.SetActive(false);
    }

}
