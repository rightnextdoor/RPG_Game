using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardLightingStrike_Controller : Explosion
{
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private Transform check;

    public void SetupLightingStrike(CharacterStats _myStats)
    {
        anim = GetComponentInChildren<Animator>();
        myStats = _myStats;      
    }

    public override void AnimationExplodeEvent()
    {

        Collider2D[] colliders = Physics2D.OverlapBoxAll(check.position, boxSize,0f, whatIsPlayer);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                isLighting = true;
                hit.GetComponent<CharacterStats>().ApplyAilments(isFire, isIce, isLighting);
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                myStats.DoDamage(hit.GetComponent<CharacterStats>());
            }
        }
    }

    private void OnDrawGizmos() => Gizmos.DrawWireCube(check.position, boxSize);
}
