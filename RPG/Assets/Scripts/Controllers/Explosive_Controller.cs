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

    private bool canGrow;

    private CircleCollider2D cd => GetComponent<CircleCollider2D>();
    private Player player;
    private float explosionTimer;
    private float moveSpeed;
    [SerializeField] private float distanceToExplosion = 1;
    private bool canMove;

    private void Update()
    {
        explosionTimer -= Time.deltaTime;

        if (explosionTimer < 0)
        {
            FinishExplosion();
        }
        if(canMove)
            MoveToPlayer();

        if (canGrow)
            transform.localScale = Vector2.Lerp(transform.lossyScale, new Vector2(maxSize, maxSize), growSpeed * Time.deltaTime);

        if (maxSize - transform.lossyScale.x < .5f)
        {
            canGrow = false;
            anim.SetTrigger("Explode");
        }

    }

    private void MoveToPlayer()
    {
        if (player == null)
            return;
        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, player.transform.position) < distanceToExplosion)
        {
            FinishExplosion();
        }
    }

    public void SetupExplosive(CharacterStats _mystats, float _growSpeed, float _maxSize, float _radius, float _moveSpeed, float _explosionTimer)
    {
        anim = GetComponent<Animator>();

        myStats = _mystats;
        growSpeed = _growSpeed;
        maxSize = _maxSize;
        explosionRadius = _radius;
        player = PlayerManager.instance.player;
        moveSpeed = _moveSpeed;
        explosionTimer = _explosionTimer;
        canMove = true;
    }

    private void AnimationExplodeEvent()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        AudioManager.instance.PlaySFX("Explosion", transform);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<CharacterStats>() != null)
            {
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                myStats.DoDamage(hit.GetComponent<CharacterStats>());         
            }
        }
    }

    private void FinishExplosion()
    {
        canGrow = true;
        canMove = false;
        //anim.SetTrigger("Explode");
    }

    private void SelfDestroy() => Destroy(gameObject);
}
