using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardLightingStrike_Controller : Explosion
{
    //[SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private Transform check;

    public void SetupLightingStrike(CharacterStats _myStats)
    {
        anim = GetComponentInChildren<Animator>();

        myStats = _myStats;
        player = PlayerManager.instance.player;
        anim.SetTrigger("Explode");
        
    }

    public override void AnimationExplodeEvent()
    {

        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, boxSize, whatIsPlayer);
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
