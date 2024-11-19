using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField] private UI ui;
    [SerializeField] private UI_Options uiOptions;
    [SerializeField] private UI_InGame uiInGame;
    [SerializeField] private UI_Checkpoint uiCheckpoint;
    

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    public UI GetUI() { return ui; }
    public UI_Options GetUIOptions() { return uiOptions; }
    public UI_InGame GetUIInGame() { return uiInGame; }
    public UI_Checkpoint GetUICheckpoint() { return uiCheckpoint; }
}
