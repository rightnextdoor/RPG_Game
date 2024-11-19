using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UI_Checkpoint : MonoBehaviour
{
    List<GameObject> uiList = new List<GameObject>();
    [SerializeField] GameObject checkpointUI;
    [SerializeField] private GameObject statsUI;

    private void Start()
    {
        SetUpList();
        DisableList();
    }

    public void OpenMenu()
    {
        if (!checkpointUI.activeSelf)
        {
            checkpointUI.SetActive(true);
            if (GameManager.instance != null)
            {
                GameManager.instance.PauseGame(true);
            }
        }
    }

    public void CloseMenu()
    {
        if (checkpointUI.activeSelf)
        {
            checkpointUI.SetActive(false);
            if (GameManager.instance != null)
            {
                GameManager.instance.PauseGame(false);
            }
        }
    }

    public void SwitchTo(GameObject _menu)
    {
        DisableList();

        if (_menu != null)
        {
            AudioManager.instance.PlaySFX("MenuClick", null);
            _menu.SetActive(true);
        }

    }

    private void SetUpList()
    {
        uiList.Add(checkpointUI);
        uiList.Add(statsUI);
    }

    private void DisableList()
    {
        foreach (GameObject item in uiList)
        {
            item.gameObject.SetActive(false);
        }
    }
}
