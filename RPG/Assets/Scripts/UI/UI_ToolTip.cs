using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_ToolTip : MonoBehaviour
{
    [SerializeField] private float xLimit;
    [SerializeField] private float yLimit;

    [SerializeField] private float xOffset = 150;
    [SerializeField] private float yOffset = 150;

    private void Start()
    {
        Debug.Log("screen height " + Screen.height);
        Debug.Log("screen width " + Screen.width);
        xLimit = Screen.width / 2;
        yLimit = Screen.height / 2;
        Debug.Log("x limit " + xLimit);
        Debug.Log("y limit " + yLimit);
    }
    private void Update()
    {
        Debug.Log("mouse pos " + Input.mousePosition);
    }
    public virtual void AdjustPosition()
    {
        Vector2 mousePosition = Input.mousePosition;

        float newXOffset = 0;
        float newYOffset = 0;

        if (mousePosition.x > xLimit)
            xOffset = -xOffset;
        else
            newXOffset = xOffset;

        if (mousePosition.y > yLimit)
            yOffset = -yOffset;
        else
            newYOffset = yOffset;

        transform.position = new Vector2(mousePosition.x + newXOffset, mousePosition.y + newYOffset);
    }

    public void AdjustFontSize(TextMeshProUGUI _text)
    {
        if (_text.text.Length > 12)
            _text.fontSize = _text.fontSize * .8f;
    }
}
