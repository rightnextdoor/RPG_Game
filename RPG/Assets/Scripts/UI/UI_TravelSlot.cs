using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_TravelSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI travelName;
    
    private Color originalColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Image image;

    private CheckpointData checkpoint;

    private void Start()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    public void SetupTravelSlot(CheckpointData _checkpoint)
    {
        if(_checkpoint == null)
            return;

        checkpoint = _checkpoint;

        travelName.text = _checkpoint.checkpointName;

        if (travelName.text.Length > 12)
            travelName.fontSize = travelName.fontSize * .7f;
        else
            travelName.fontSize = 24;
    }



    public void OnPointerDown(PointerEventData eventData)
    {
        CheckpointManager.instance.UpdateLastSaveCheckpoint(checkpoint);
        CheckpointManager.instance.TravelTo(checkpoint);
        UIManager.instance.GetUICheckpoint().CloseMenu();
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
