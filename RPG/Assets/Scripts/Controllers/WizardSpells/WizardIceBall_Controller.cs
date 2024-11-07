using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardIceBall_Controller : Explosion
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private Rigidbody2D rb;

    private float explosionTimer;
    [SerializeField] private float distanceToExplosion = 1;
    private bool canGrow;
    private float growSpeed = 15;
    private float maxSize = 6;

    private void Update()
    {
        explosionTimer -= Time.deltaTime;

        if (explosionTimer < 0)
        {
            FinishExplosion();
        }

        if (canMove)
            MoveToPlayer();

        if (canGrow)
            transform.localScale = Vector2.Lerp(transform.lossyScale, new Vector2(maxSize, maxSize), growSpeed * Time.deltaTime);

        if (maxSize - transform.lossyScale.x < .5f)
        {
            canGrow = false;
            anim.SetTrigger("Explode");
        }
    }

    public void SetupIceBall(float _speed, CharacterStats _myStats, float _explosionTimer, float _growSpeed, float _maxSize, float _radius)
    {
        anim = GetComponentInChildren<Animator>();

        moveSpeed = _speed;
        myStats = _myStats;
        growSpeed = _growSpeed;
        maxSize = _maxSize;
        explosionRadius = _radius;
        explosionTimer = _explosionTimer;
        player = PlayerManager.instance.player;

        canMove = true;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(targetLayerName))
        {
            Explode();
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        canGrow = true;
        canMove = false;
    }
    public override void AnimationExplodeEvent()
    {
        isIce = true;
        base.AnimationExplodeEvent();
        //AudioManager.instance.PlaySFX("BubbleImpact", transform);
    }

    private void FinishExplosion()
    {
        canMove = false;
        anim.SetTrigger("Explode");
    }
}
