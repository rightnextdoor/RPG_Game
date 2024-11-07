using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public bool canMove;
    [HideInInspector] public CharacterStats myStats;
    [HideInInspector] public float explosionRadius;
    [HideInInspector] public bool isFire;
    [HideInInspector] public bool isIce;
    [HideInInspector] public bool isLighting;
    public virtual void AnimationExplodeEvent()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
 
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<CharacterStats>() != null)
            {
                if (hit.gameObject.tag == "Player")
                {
                    hit.GetComponent<CharacterStats>().ApplyAilments(isFire, isIce, isLighting);
                    hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                    myStats.DoDamage(hit.GetComponent<CharacterStats>());
                }
                
            }
        }
    }

    public virtual void SelfDestroy() => Destroy(gameObject);
}
