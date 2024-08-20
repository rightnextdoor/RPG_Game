using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone_Skill : Skill
{
    [Header("Clone info")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float cloneDuration;
    [Space]
    [SerializeField] private bool canAttack;

    [Header("Clone can duplicate")]
    [SerializeField] private bool canDuplicateClone;
    [SerializeField] private float chanceToDuplicate;

    [Header("Crystal instead of clone")]
    public bool crystalInseadOfClone;

    public void CreateClone(Transform _clonePosition, Vector3 _offset)
    {
        if (crystalInseadOfClone)
        {
            SkillManager.instance.crystal.CreateCrystal();        
            return;
        }

        GameObject newClone = Instantiate(clonePrefab);

        newClone.GetComponent<Clone_Skill_Controller>().
            SetupClone(_clonePosition, cloneDuration, canAttack, _offset, FindClosestEnemy(newClone.transform), canDuplicateClone, chanceToDuplicate,player);
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
