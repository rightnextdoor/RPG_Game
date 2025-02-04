using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dash_Skill : Skill
{
    public bool dashUnlocked {  get; private set; }
    public bool cloneOnDashUnlocked { get; private set; }
    public bool cloneOnArrivalUnlocked {  get; private set; }

    public override void UseSkill()
    {     
        base.UseSkill();
        AudioManager.instance.PlaySFX("Dash", null);

        cooldown = PlayerManager.instance.player.dashDuration + 0.3f;
    }

    protected override void Start()
    {
        base.Start();
    }

    public override void CheckUnlock(List<SkillData> skilldata)
    {
        foreach (SkillData data in skilldata)
        {
            if (data.fileName == "Dash")
            {
                UnlockDash(data.unlocked);
            }
            if (data.fileName == "DashClone")
            {
                UnlockCloneOnDash(data.unlocked);
            }
            if (data.fileName == "DashArrival")
            {
                UnlockCloneOnArrival(data.unlocked);
            }
        }
    
    }

    private void UnlockDash(bool unlock)
    {
        dashUnlocked = unlock;
    }

    private void UnlockCloneOnDash(bool unlock)
    {
        cloneOnDashUnlocked = unlock;
    }

    private void UnlockCloneOnArrival(bool unlock) 
    {
        cloneOnArrivalUnlocked = unlock;
    }
    public void CloneOnDash()
    {
        if (cloneOnDashUnlocked)
            SkillManager.instance.clone.CreateClone(player.transform, Vector3.zero);
    }

    public void CloneOnArrival()
    {
        if (cloneOnArrivalUnlocked)
            SkillManager.instance.clone.CreateClone(player.transform, Vector3.zero);
    }
}
