using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonWarrior_FireBlast_Controller : Explosion
{
    [SerializeField] private float xVelocity;
    [SerializeField] private Rigidbody2D rb;

    private float explosionTimer;

    private void Update()
    {
        explosionTimer -= Time.deltaTime;

        if (explosionTimer < 0)
        {
            FinishExplosion();
        }

        if (canMove)
            rb.linearVelocity = new Vector2(xVelocity, rb.linearVelocity.y);
    }

    public void SetupFireBlast(float _speed, CharacterStats _myStats, float _radius, float _explosionTimer)
    {
        anim = GetComponentInChildren<Animator>();

        xVelocity = _speed;
        myStats = _myStats;
        explosionRadius = _radius;
        explosionTimer = _explosionTimer;
        player = PlayerManager.instance.player;

        if (xVelocity < 0)
            transform.Rotate(0, 180, 0);

        canMove = true;
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
        canMove = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        AnimationExplodeEvent();
        SelfDestroy();
    }
    public override void AnimationExplodeEvent()
    {
        isFire = true;
        base.AnimationExplodeEvent();
    }

    private void FinishExplosion()
    {
        canMove = false;
    }
}
