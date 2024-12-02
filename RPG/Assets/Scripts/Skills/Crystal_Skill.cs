using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crystal_Skill : Skill
{
    [SerializeField] private float crystalDuration;
    [SerializeField] private GameObject crystalPrefab;
    private GameObject currentCrystal;

    [Header("Clone Spawn")]
    [SerializeField] private bool cloneInsteadOfCrystal;

    public bool crystalUnlocked {  get; private set; }

    [Header("Explosive crystal")]
    [SerializeField] private float explosiveCooldown;
    [SerializeField] private bool canExplode;

    [Header("Moving crystal")]
    [SerializeField] private bool canMoveToEnemy;
    [SerializeField] private float moveSpeed;

    [Header("Multi stacking crystal")]
    [SerializeField] private bool canUseMultiStacks;
    [SerializeField] private int amountOfStacks;
    [SerializeField] private float multiStackCooldown;
    [SerializeField] private float useTimeWindow;
    [SerializeField] private List<GameObject> crystalLeft = new List<GameObject>();

    protected override void Start()
    {
        base.Start();
    }

    #region Unlock skill region

    public override void CheckUnlock(List<SkillData> skilldata)
    {
        foreach (SkillData data in skilldata)
        {
            if (data.fileName == "Crystal")
            {
                UnlockCrystal(data.unlocked);
            }
            if (data.fileName == "CloneSpawn")
            {
                UnlockCrystalMirage(data.unlocked);
            }
            if (data.fileName == "CrystalExplosion")
            {
                UnlockExplosiveCrystal(data.unlocked);
            }
            if (data.fileName == "CrystalMove")
            {
                UnlockMovingCrystal(data.unlocked);
            }
            if (data.fileName == "CrystalMulti")
            {
                UnlockMultiStack(data.unlocked);
            }
        }   
        
    }

    private void UnlockCrystal(bool unlock)
    {
        crystalUnlocked = unlock;
    }

    private void UnlockCrystalMirage(bool unlock)
    {
        cloneInsteadOfCrystal = unlock;
    }
    private void UnlockExplosiveCrystal(bool unlock)
    {
        canExplode = unlock;
        
        if (canExplode)
        {          
            cooldown = explosiveCooldown;
        }
    }
    private void UnlockMovingCrystal(bool unlock)
    {
        canMoveToEnemy = unlock;
    }
    private void UnlockMultiStack(bool unlock)
    {
        canUseMultiStacks = unlock;
    }

    #endregion

    public override void UseSkill()
    {
        base.UseSkill();

        if (CanUseMultiCrystal())
            return;

        if(currentCrystal == null)
        {
            CreateCrystal();
        }
        else
        {
            if (canMoveToEnemy)
                return;

            Vector2 playerPos = player.transform.position;
            player.transform.position = currentCrystal.transform.position;
            currentCrystal.transform.position = playerPos;

            if(cloneInsteadOfCrystal)
            {
                SkillManager.instance.clone.CreateClone(currentCrystal.transform, Vector3.zero);
                Destroy(currentCrystal);
            }
            else
            {
                currentCrystal.GetComponent<Crystal_Skill_Controller>()?.FinishCrystal();
            }
        }
    }

    public void CreateCrystal()
    {
        currentCrystal = Instantiate(crystalPrefab, player.transform.position, Quaternion.identity);
        Crystal_Skill_Controller currentCrystalScript = currentCrystal.GetComponent<Crystal_Skill_Controller>();

        currentCrystalScript.SetupCrystal(crystalDuration, canExplode, canMoveToEnemy, moveSpeed, FindClosestEnemy(currentCrystal.transform), player);

    }

    public void CurrentCrystalChooseRandomTarget() => currentCrystal.GetComponent<Crystal_Skill_Controller>().ChooseRandomEnemy();

    private bool CanUseMultiCrystal()
    {
        if (canUseMultiStacks)
        {
            if(crystalLeft.Count > 0)
            {
                if (crystalLeft.Count == amountOfStacks)
                    Invoke("ResetAbility", useTimeWindow);

                cooldown = 0;
                GameObject crystalToSpawn = crystalLeft[crystalLeft.Count - 1];
                GameObject newCrystal = Instantiate(crystalToSpawn, player.transform.position, Quaternion.identity);

                crystalLeft.Remove(crystalToSpawn);

                newCrystal.GetComponent<Crystal_Skill_Controller>().
                    SetupCrystal(crystalDuration, canExplode, canMoveToEnemy, moveSpeed, FindClosestEnemy(newCrystal.transform), player);

                if(crystalLeft.Count <= 0)
                {
                    cooldown = multiStackCooldown;
                    RefilCrystal();
                }
                return true;
            }

        }
        return false;
    }

    private void RefilCrystal()
    {
        int amountToAdd = amountOfStacks - crystalLeft.Count;

        for(int i = 0; i < amountToAdd; i++)
        {
            crystalLeft.Add(crystalPrefab);
        }
    }

    private void ResetAbility()
    {
        if (cooldown > 0)
            return;

        cooldownTimer = multiStackCooldown;
        RefilCrystal();
    }
}
