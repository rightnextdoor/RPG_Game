using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField] private UI ui;
    [SerializeField] private UI_Options uiOptions;
    [SerializeField] private UI_InGame uiInGame;
    [SerializeField] private UI_Checkpoint uiCheckpoint;
    [SerializeField] private UI_Buttons uiButtons;
    [SerializeField] private UI_GameOver uiGameOver;
    [SerializeField] private UI_BossHealth uiBossHealth;
    [SerializeField] private UI_Notification uiNotification;
    [SerializeField] private UI_Inventory uiInventory;
    [SerializeField] private UI_ToolTipUI uiToolTipUI;
    [SerializeField] private UI_Player uiPlayer;
    [SerializeField] private UI_LevelSystem uiLevelSystem;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public UI GetUI() => ui;
    public UI_Options GetUIOptions() => uiOptions;
    public UI_InGame GetUIInGame() => uiInGame;
    public UI_Checkpoint GetUICheckpoint() => uiCheckpoint;
    public UI_Buttons GetUIButtons() => uiButtons;
    public UI_GameOver GetUIGameOver() => uiGameOver;
    public UI_BossHealth GetUIBossHealth() => uiBossHealth;
    public UI_Notification GetUINotification() => uiNotification;
    public UI_Inventory GetUIInventory() => uiInventory;
    public UI_ToolTipUI GetUIToolTipUI() => uiToolTipUI;
    public UI_Player GetUIPlayer() => uiPlayer;
    public UI_LevelSystem GetUILevelSystem() => uiLevelSystem;
}
