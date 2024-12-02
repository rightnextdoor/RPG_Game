using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Parry_Skill : Skill
{
    public bool parryUnlocked { get; private set; }

    [Header("Parry restore")]
    [Range(0f,1f)]
    [SerializeField] private float restoreHealthPerentage;
    public bool restoreUnlocked { get; private set; }

    public bool parryWithMirageUnlocked { get; private set; }

    public override void UseSkill()
    {
        base.UseSkill();

        if (restoreUnlocked)
        {
            int restoreAmount = Mathf.RoundToInt(player.stats.GetMaxHealthValue() * restoreHealthPerentage);
            player.stats.IncreaseHealthBy(restoreAmount);
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    public override void CheckUnlock(List<SkillData> skilldata)
    {
        foreach (SkillData data in skilldata)
        {
            if (data.fileName == "Parry")
            {
                UnlockParry(data.unlocked);
            }
            if (data.fileName == "ParryRestore")
            {
                UnlockParryRestore(data.unlocked);
            }
            if (data.fileName == "ParryMirage")
            {
                UnlockParryWithMirage(data.unlocked);
            }
        }    
        
    }

    private void UnlockParry(bool unlock)
    {
        parryUnlocked = unlock;
    }

    private void UnlockParryRestore(bool unlock)
    {
        restoreUnlocked = unlock;
    }

    private void UnlockParryWithMirage(bool unlock)
    {
        parryWithMirageUnlocked = unlock;
    }

    public void MakeMirageOnParry(Transform _respawnTransform)
    {
        if (parryWithMirageUnlocked)
            SkillManager.instance.clone.CreateCloneWithDelay(_respawnTransform);
    }
}
