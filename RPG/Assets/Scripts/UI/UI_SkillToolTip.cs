using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_SkillToolTip : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] GameObject background;
    [SerializeField] private TextMeshProUGUI skillDescprtion;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillCost;
    private RectTransform backgroundRectTransform;
    private RectTransform rectTransform;

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

    private void SetText(string tooltipString)
    {
        skillDescprtion.SetText(tooltipString);
        skillDescprtion.ForceMeshUpdate();

        //Vector2 textSize = skillDescprtion.GetRenderedValues(false);
        //Vector2 paddingSize = skillDescprtion.margin * 2;
        //backgroundRectTransform.sizeDelta = textSize + paddingSize;
    }

    public void ShowTooltip(string _skillDescprtion, string _skillName, int _price)
    {
        gameObject.SetActive(true);

        skillName.SetText(_skillName);
        skillCost.SetText("Cost: " + _price);

        SetText(_skillDescprtion);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

}
