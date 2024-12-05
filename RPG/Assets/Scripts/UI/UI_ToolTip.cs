using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ToolTip : MonoBehaviour
{

    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] GameObject background;
    [SerializeField]private TextMeshProUGUI tooltipText;
    private RectTransform backgroundRectTransform;
    private RectTransform rectTransform;

    //private System.Func<string> getTooltipTextFunc;

    private void Awake()
    {

        backgroundRectTransform = background.GetComponent<RectTransform>();
        rectTransform = transform.GetComponent<RectTransform>();

        HideTooltip();
    }

    private void Update()
    {
        //SetText(getTooltipTextFunc());

        Vector2 anchoredPosition = Input.mousePosition / canvasRectTransform.localScale.x;

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, 0, canvasRectTransform.rect.width - backgroundRectTransform.rect.width);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0, canvasRectTransform.rect.height - backgroundRectTransform.rect.height);

        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void SetText(string tooltipString)
    {
        tooltipText.SetText(tooltipString);
        tooltipText.ForceMeshUpdate();

        Vector2 textSize = tooltipText.GetRenderedValues(false);
        Vector2 paddingSize = tooltipText.margin * 2;
        backgroundRectTransform.sizeDelta = textSize + paddingSize;
    }

    //private void ShowTooltip(System.Func<string> getTooltipTextFunc)
    //{
    //    this.getTooltipTextFunc = getTooltipTextFunc;
    //    gameObject.SetActive(true);
    //    SetText(getTooltipTextFunc());
    //}

    public void ShowTooltip(string tooltipString)
    {
        gameObject.SetActive(true);
        SetText(tooltipString);
    }  

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    //public static void ShowTooltip_Static(System.Func<string> getTooltipTextFunc)
    //{
    //    Instance.ShowTooltip(getTooltipTextFunc);
    //}

}
