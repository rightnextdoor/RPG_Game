using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UI_Checkpoint : MonoBehaviour
{
    List<GameObject> uiList = new List<GameObject>();
    [SerializeField] GameObject checkpointUI;
    [SerializeField] private GameObject statsUI;
    [SerializeField] private GameObject travelUI;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject skillUI;
    [SerializeField] private GameObject craftUI;

    [SerializeField] private GameObject travelButton;

    [SerializeField] private GameObject shopWeapon;
    [SerializeField] private GameObject craftWeapon;

    public UI_ShopWindow shopWindow;
    public UI_CraftWindow craftWindow;

    private void Start()
    {
        CloseMenu();
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
        if (GameManager.instance.TravelCheckpoints().Count == 0)
        {
            travelButton.SetActive(false);
        }
        else
        {
            travelButton.SetActive(true) ;
        }
    }

    public void CloseMenu()
    {
        if (checkpointUI.activeSelf)
        {
            checkpointUI.SetActive(false);
            DisableList();
            if (GameManager.instance != null)
            {
                GameManager.instance.PauseGame(false);
            }
        }
    }

    public void SwitchTo(GameObject _menu)
    {
        if (_menu != null)
        {
            if (_menu.activeSelf)
                return;
        }

        DisableList();

        if (_menu.GetComponent<UI_TravelList>() != null)
        {
            _menu.GetComponent<UI_TravelList>().SetupTravelList();
        }

        if (_menu != null)
        {
            AudioManager.instance.PlaySFX("MenuClick", null);
            _menu.SetActive(true);

            if (_menu.GetComponent<UI_Stats>() != null)
            {
                _menu.GetComponent<UI_Stats>().StatsCalled();
            }

            if (_menu == shopUI)
            {
                if (shopWeapon.GetComponent<UI_ShopList>() != null)
                    shopWeapon.GetComponent<UI_ShopList>().CallShop();           
            }

            if (_menu == craftUI)
            {
                if(craftWeapon.GetComponent<UI_CraftList>() != null)
                    craftWeapon.GetComponent<UI_CraftList>().CallCraft();
            }

        }

    }

    private void SetUpList()
    {
        uiList.Add(statsUI);
        uiList.Add(travelUI);
        uiList.Add(shopUI);
        uiList.Add(skillUI);
        uiList.Add(craftUI);
    }

    private void DisableList()
    {
        foreach (GameObject item in uiList)
        {
            item.gameObject.SetActive(false);
        }
    }
}
