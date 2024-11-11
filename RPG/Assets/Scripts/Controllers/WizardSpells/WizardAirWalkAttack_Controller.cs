using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class WizardAirWalkAttack_Controller : Explosion
{
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private Transform check;
    [SerializeField] private List<SpriteLibraryAsset> sprites = new List<SpriteLibraryAsset>();
    [SerializeField] private SpriteLibrary spriteLibrary;

    public void SetupAirWalkAttack(CharacterStats _myStats, int _spriteSelected)
    {
        anim = GetComponentInChildren<Animator>();
        myStats = _myStats;

        spriteLibrary.spriteLibraryAsset = sprites[_spriteSelected];
    }

    public override void AnimationExplodeEvent()
    {

        Collider2D[] colliders = Physics2D.OverlapBoxAll(check.position, boxSize, 0f, whatIsPlayer);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                isFire = true;
                hit.GetComponent<CharacterStats>().ApplyAilments(isFire, isIce, isLighting);
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                myStats.DoDamage(hit.GetComponent<CharacterStats>());
            }
        }
    }

    private void OnDrawGizmos() => Gizmos.DrawWireCube(check.position, boxSize);
}
