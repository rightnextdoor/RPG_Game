using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour, ISaveManager
{
    [SerializeField] private GameObject charcaterUI;
    [SerializeField] private GameObject skillTreeUI;
    [SerializeField] private GameObject craftUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject gameOverUI;

    [SerializeField] private GameObject checkpointUI;

    private List<GameObject> uiList = new List<GameObject>();

    [SerializeField] private GameObject craftWeapon;

    public UI_CraftWindow craftWindow;

    [SerializeField] private UI_VolumeSlider[] volumeSettings;

    void Start()
    {
        SetUpUIList();
        SwitchTo(inGameUI);
    }

    void Update()
    {
        if (checkpointUI.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.C))
            SwitchWithKeyTo(charcaterUI);

        if (Input.GetKeyDown(KeyCode.B))
            SwitchWithKeyTo(craftUI);

        if (Input.GetKeyDown(KeyCode.K))
            SwitchWithKeyTo(skillTreeUI);

        if (Input.GetKeyDown(KeyCode.O))
            SwitchWithKeyTo(optionsUI);


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inGameUI.activeSelf)
            {
                SwitchWithKeyTo(charcaterUI);
            }
            else
            {
                CloseMenu();
            }
        }
    }

    private void SetUpUIList()
    {
        uiList.Add(charcaterUI);
        uiList.Add(skillTreeUI);
        uiList.Add(craftUI);
        uiList.Add(optionsUI);
        uiList.Add(inGameUI);
        uiList.Add(gameOverUI);
    }

    public void CloseMenu()
    {
        if (!inGameUI.activeSelf)
        {
            SwitchTo(inGameUI);
        }
    }

    public void SwitchTo(GameObject _menu)
    {
        ToolTipManager.instance.HideToolTip();
        foreach (GameObject list in uiList)
        {
            list.gameObject.SetActive(false);
        }

        if (_menu != null)
        {
            AudioManager.instance.PlaySFX("MenuClick", null);
            _menu.SetActive(true);

            if (_menu == craftUI)
            {
                if (craftWeapon.GetComponent<UI_CraftList>() != null)
                    craftWeapon.GetComponent<UI_CraftList>().CallCraft();
            }
        }

        if (GameManager.instance != null)
        {
            if (_menu == inGameUI)
            {
                GameManager.instance.PauseGame(false);
            }
            else
            {
                GameManager.instance.PauseGame(true);
            }
        }
    }

    public void SwitchWithKeyTo(GameObject _menu)
    {
        if (_menu != null && _menu.activeSelf)
        {
            _menu.SetActive(false);
            CheckForInGameUI();
            return;
        }
        SwitchTo(_menu);

    }

    private void CheckForInGameUI()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                return;
        }

        SwitchTo(inGameUI);
    }

    public void LoadData(GameData _data)
    {
        foreach (KeyValuePair<string, float> pair in _data.volumeSettings)
        {
            foreach (UI_VolumeSlider item in volumeSettings)
            {
                if (item.parameter == pair.Key)
                    item.LoadSlider(pair.Value);
            }
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.volumeSettings.Clear();

        foreach (UI_VolumeSlider item in volumeSettings)
        {
            _data.volumeSettings.Add(item.parameter, item.slider.value);
        }
    }
}
