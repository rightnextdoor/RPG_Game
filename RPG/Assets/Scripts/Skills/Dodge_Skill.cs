using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dodge_Skill : Skill
{
    [Header("Dodge")]
    [SerializeField] private int evasionAmount;
    public bool dodgeUnlocked;

    [Header("Mirage dodge")]
    public bool dodgeMirageUnlocked;

    protected override void Start()
    {
        base.Start();
    }

    public override void CheckUnlock(List<SkillData> skilldata)
    {
        foreach (SkillData data in skilldata)
        {
            if (data.fileName == "Dodge")
            {
                UnlockDodge(data.unlocked);
            }
            if (data.fileName == "DodgeMirage")
            {
                UnlockMirageDodge(data.unlocked);
            }
        }

        
        
    }

    private void UnlockDodge(bool unlock)
    {
        if (unlock)
        {
            StartCoroutine(UnlockDodgeDelay(.1f));
        } else
        {
            dodgeUnlocked = false;
        }
    }

    private IEnumerator UnlockDodgeDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        player.stats.evasion.AddModifier(evasionAmount);
        Inventory.instance.UpdateStatsUI();
        dodgeUnlocked = true;
    }

    private void UnlockMirageDodge(bool unlock)
    {
        dodgeMirageUnlocked = unlock;
    }

    public void CreateMirageOnDodge()
    {
        if (dodgeMirageUnlocked)
            SkillManager.instance.clone.CreateClone(player.transform, new Vector3(2 * player.facingDir,0));
    }

}
