using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Clone_Skill : Skill
{
    [Header("Clone info")]
    [SerializeField] private float attackMultiplier;
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float cloneDuration;
    [Space]

    [Header("Clone attack")]
    [SerializeField] private float cloneAttackMultiplier;
    [SerializeField] private bool canAttack;

    [Header("Aggresive clone")]
    [SerializeField] private float aggresiveCloneArrackMultiplier;
    public bool canApplyOnHitEffect {get; private set;}

    [Header("Multiple clone")]
    [SerializeField] private float multiCloneAttackMultiplier;
    [SerializeField] private bool canDuplicateClone;
    [SerializeField] private float chanceToDuplicate;

    [Header("Crystal spawn")]
    public bool crystalInseadOfClone;

    protected override void Start()
    {
        base.Start();

    }

    #region Unlock region

    public override void CheckUnlock(List<SkillData> skilldata)
    {
        foreach (SkillData data in skilldata)
        {
            if (data.fileName == "CloneAttack")
            {
                UnlockCloneAttack(data.unlocked);
            }
            if (data.fileName == "CloneAggresive")
            {
                UnlockAggresiveClone(data.unlocked);
            }
            if (data.fileName == "CloneMultiple")
            {
                UnlockMultiClone(data.unlocked);
            }
            if (data.fileName == "CrystalSpawn")
            {
                UnlockCrystalInstead(data.unlocked);
            }
        }
  
    }

    private void UnlockCloneAttack(bool unlock)
    {
        canAttack = unlock;
        
        if (canAttack)
        {          
            attackMultiplier = cloneAttackMultiplier;
        }
    }

    private void UnlockAggresiveClone(bool unlock)
    {
        canApplyOnHitEffect = unlock;
        
        if (canApplyOnHitEffect)
        {           
            attackMultiplier = aggresiveCloneArrackMultiplier;
        }
    }

    private void UnlockMultiClone(bool unlock)
    {
        canDuplicateClone = unlock;

        if (canDuplicateClone)
        {         
            attackMultiplier = multiCloneAttackMultiplier;
        }
    }

    private void UnlockCrystalInstead(bool unlock)
    {
        if(unlock)
            crystalInseadOfClone = true;
    }

    #endregion

    public void CreateClone(Transform _clonePosition, Vector3 _offset)
    {
        if (crystalInseadOfClone)
        {
            SkillManager.instance.crystal.CreateCrystal();        
            return;
        }

        GameObject newClone = Instantiate(clonePrefab);

        newClone.GetComponent<Clone_Skill_Controller>().
            SetupClone(_clonePosition, cloneDuration, canAttack, _offset, canDuplicateClone, chanceToDuplicate,player,attackMultiplier);
    }

    public void CreateCloneWithDelay(Transform _enemyTransform)
    {
       StartCoroutine(CloneDelayCorotine(_enemyTransform, new Vector3(2 * player.facingDir, 0)));
    }

    private IEnumerator CloneDelayCorotine(Transform _transfrom, Vector3 _offset)
    {
        yield return new WaitForSeconds(.4f);
            CreateClone(_transfrom, _offset);
    }
}
