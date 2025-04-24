using System.Collections.Generic;
using UnityEngine;

public class UI_Checkpoint : MonoBehaviour
{
    [Header("Main UI Panels")]
    [SerializeField] private GameObject checkpointUI;
    [SerializeField] private GameObject statsUI;
    [SerializeField] private GameObject travelUI;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject skillUI;
    [SerializeField] private GameObject craftUI;

    [Header("UI Buttons")]
    [SerializeField] private GameObject travelButton;

    [Header("Weapon Slots")]
    [SerializeField] private GameObject shopWeapon;
    [SerializeField] private GameObject craftWeapon;

    [Header("Windows")]
    [SerializeField] public UI_ShopWindow shopWindow;
    [SerializeField] public UI_CraftWindow craftWindow;

    private readonly List<GameObject> uiList = new();
    private bool isMenuOpen;

    private void Start()
    {
        SetUpList();
        DisableList();
        CloseMenu();
    }

    public void OpenMenu()
    {
        isMenuOpen = true;

        if (checkpointUI != null && !checkpointUI.activeSelf)
        {
            checkpointUI.SetActive(true);
            GameManager.instance?.PauseGame(true);
        }

        if (travelButton != null)
            travelButton.SetActive(CheckpointManager.instance.GetActivatedCheckpoints().Count > 0);
    }

    public void CloseMenu()
    {
        isMenuOpen = false;

        if (checkpointUI != null && checkpointUI.activeSelf)
        {
            checkpointUI.SetActive(false);
            DisableList();
            GameManager.instance?.PauseGame(false);
        }
    }

    public void SwitchTo(GameObject _menu)
    {
        if (_menu == null || _menu.activeSelf)
            return;

        DisableList();

        if (_menu.TryGetComponent(out UI_TravelList travelList))
            travelList.SetupTravelList();

        if (_menu.TryGetComponent(out UI_Stats stats))
            stats.StatsCalled();

        if (_menu == shopUI && shopWeapon.TryGetComponent(out UI_ShopList shopList))
            shopList.CallShop();

        if (_menu == craftUI && craftWeapon.TryGetComponent(out UI_CraftList craftList))
            craftList.CallCraft();

        AudioManager.instance?.PlaySFX("MenuClick");
        _menu.SetActive(true);
    }

    public bool IsMenuOpen() => isMenuOpen;

    private void SetUpList()
    {
        uiList.Clear();
        if (statsUI != null) uiList.Add(statsUI);
        if (travelUI != null) uiList.Add(travelUI);
        if (shopUI != null) uiList.Add(shopUI);
        if (skillUI != null) uiList.Add(skillUI);
        if (craftUI != null) uiList.Add(craftUI);
    }

    private void DisableList()
    {
        foreach (var ui in uiList)
        {
            if (ui != null)
                ui.SetActive(false);
        }
    }
}
