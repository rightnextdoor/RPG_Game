using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipManager : MonoBehaviour
{
    public static ToolTipManager instance;

    public UI_ToolTip toolTip;
    public UI_ItemToolTip itemToolTip;
    public UI_SkillToolTip skillToolTip;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;

    }

    public void HideToolTip()
    {
        toolTip.HideTooltip();
        itemToolTip.HideItemTooltip();
        skillToolTip.HideTooltip();
    }
}
