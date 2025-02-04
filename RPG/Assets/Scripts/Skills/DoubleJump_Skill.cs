using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJump_Skill : Skill
{
    public bool doubleJumpUnlocked { get; private set; }

    public override void UseSkill()
    {
        base.UseSkill();
    }

    protected override void Start()
    {
        base.Start();
    }
    public override void CheckUnlock(List<SkillData> skilldata)
    {
        foreach (SkillData data in skilldata)
        {
            if (data.fileName == "DoubleJump")
            {
                UnlockDoubleJump(data.unlocked);
            }
        }
    }

    private void UnlockDoubleJump(bool unlock)
    {
        doubleJumpUnlocked = unlock;
    }

    public override bool CanUseSkill()
    {
        if(player.canDoubleJump)
            return true;
        else 
            return false;
    }
}
