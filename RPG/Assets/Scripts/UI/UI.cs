using System.Collections.Generic;
using UnityEngine;

public class UI : MonoBehaviour
{
    [Header("Main UI Panels")]
    [SerializeField] private GameObject charcaterUI;
    [SerializeField] private GameObject skillTreeUI;
    [SerializeField] private GameObject craftUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject checkpointUI;

    [Header("Crafting")]
    [SerializeField] private GameObject craftWeapon;
    public UI_CraftWindow craftWindow;

    private List<GameObject> uiList = new();

    private void Start()
    {
        SetUpUIList();
        SwitchTo(inGameUI);
    }

    private void Update()
    {
        if (checkpointUI != null && checkpointUI.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.C)) SwitchWithKeyTo(charcaterUI);
        if (Input.GetKeyDown(KeyCode.B)) SwitchWithKeyTo(craftUI);
        if (Input.GetKeyDown(KeyCode.K)) SwitchWithKeyTo(skillTreeUI);
        if (Input.GetKeyDown(KeyCode.O)) SwitchWithKeyTo(optionsUI);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inGameUI != null && inGameUI.activeSelf)
                SwitchWithKeyTo(charcaterUI);
            else
                CloseMenu();
        }
    }

    private void SetUpUIList()
    {
        uiList.Clear();
        uiList.Add(charcaterUI);
        uiList.Add(skillTreeUI);
        uiList.Add(craftUI);
        uiList.Add(optionsUI);
        uiList.Add(inGameUI);
        uiList.Add(gameOverUI);
    }

    public void CloseMenu()
    {
        if (inGameUI != null && !inGameUI.activeSelf)
        {
            SwitchTo(inGameUI);
        }
    }

    public void SwitchTo(GameObject _menu)
    {
        if (_menu != null && _menu.activeSelf)
            return; // Don't switch if the menu is already active

        ToolTipManager.instance?.HideToolTip();

        foreach (var ui in uiList)
        {
            if (ui != null)
                ui.SetActive(false);
        }

        if (_menu != null)
        {
            if (_menu != inGameUI)
                AudioManager.instance?.PlaySFX("MenuClick");

            _menu.SetActive(true);

            if (_menu == craftUI && craftWeapon != null && craftWeapon.TryGetComponent(out UI_CraftList craftList))
                craftList.CallCraft();
        }

        GameManager.instance?.PauseGame(_menu != inGameUI);
    }

    public void SwitchWithKeyTo(GameObject _menu)
    {
        if (_menu == null) return;

        if (_menu.activeSelf)
        {
            _menu.SetActive(false);
            CheckForInGameUI();
        }
        else
        {
            SwitchTo(_menu);
        }
    }

    private void CheckForInGameUI()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
                return;
        }

        SwitchTo(inGameUI);
    }
}
