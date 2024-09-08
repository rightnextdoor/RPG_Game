using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive_Controller : MonoBehaviour
{
    private Animator anim;
    private CharacterStats myStats;
    private float growSpeed = 15;
    private float maxSize = 6;
    private float explosionRadius;

    private bool canGrow = true;

    private void Update()
    {
        if (canGrow)
            transform.localScale = Vector2.Lerp(transform.lossyScale, new Vector2(maxSize, maxSize), growSpeed * Time.deltaTime);
        
        if (maxSize - transform.lossyScale.x < .5f)
        {
            canGrow = false;
            anim.SetTrigger("Explode");
        }
    }

    public void SetupExplosive(CharacterStats _mystats, float _growSpeed, float _maxSize, float _radius)
    {
        anim = GetComponent<Animator>();

        myStats = _mystats;
        growSpeed = _growSpeed;
        maxSize = _maxSize;
        explosionRadius = _radius;
    }

    private void AnimationExplodeEvent()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
      
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<CharacterStats>() != null)
            {
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                myStats.DoDamage(hit.GetComponent<CharacterStats>());         
            }
        }
    }

    private void SelfDestroy() => Destroy(gameObject);
}
