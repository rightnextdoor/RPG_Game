using System.Collections;
using UnityEngine;

public class SpecialDamage : MonoBehaviour
{
    #region Setup

    private CharacterStats myStats;

    public void Setup(CharacterStats _myStats)
    {
        myStats = _myStats;
    }

    #endregion
    #region Damage
    public void DoDamage(Collider2D[] targets)
    {
        if (targets == null || myStats == null)
            return;

        foreach (Collider2D target in targets)
        {
            if (target == null)
                continue;

            CharacterStats targetStats = target.GetComponent<CharacterStats>();
            if (targetStats == null)
                continue;

            myStats.DoDamage(targetStats);
        }
    }
    #endregion
    #region Destroy
    public void SelfDestroy() => Destroy(gameObject);

    public void DestroyAfter(float time, bool useOwnerDeath)
    {
        StartCoroutine(DestroyAfterCoroutine(time, useOwnerDeath));
    }

    private IEnumerator DestroyAfterCoroutine(float time, bool useOwnerDeath)
    {
        float timer = time;

        while (timer > 0)
        {
            if (useOwnerDeath && myStats != null && myStats.isDead)
            {
                Destroy(gameObject);
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
    #endregion
}