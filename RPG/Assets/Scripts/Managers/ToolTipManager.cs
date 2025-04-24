using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToolTipManager : MonoBehaviour
{
    public static ToolTipManager instance;

    public UI_ToolTip toolTip;
    public UI_ItemToolTip itemToolTip;
    public UI_SkillToolTip skillToolTip;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(WaitForUIManagerAndAssign());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(WaitForUIManagerAndAssign());
    }

    private IEnumerator WaitForUIManagerAndAssign()
    {
        yield return new WaitUntil(() =>
            UIManager.instance != null && UIManager.instance.GetUIToolTipUI() != null);

        var ui = UIManager.instance.GetUIToolTipUI();

        toolTip = ui.toolTip;
        itemToolTip = ui.itemToolTip;
        skillToolTip = ui.skillToolTip;
    }

    public void HideToolTip()
    {
        toolTip?.HideTooltip();
        itemToolTip?.HideItemTooltip();
        skillToolTip?.HideTooltip();
    }
}
