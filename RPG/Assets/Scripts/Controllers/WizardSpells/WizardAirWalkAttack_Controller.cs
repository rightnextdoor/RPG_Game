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
    [SerializeField] private BoxCollider2D boxCollider;

    public void SetupAirWalkAttack(CharacterStats _myStats, int _spriteSelected)
    {
        anim = GetComponentInChildren<Animator>();
        myStats = _myStats;
        AudioManager.instance.PlaySFX("Wizard_AirWalkAttack");
        spriteLibrary.spriteLibraryAsset = sprites[_spriteSelected];
    }

    public override void AnimationExplodeEvent()
    {        
        boxCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
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
    }

    private void OnDrawGizmos() => Gizmos.DrawWireCube(check.position, boxSize);
}
