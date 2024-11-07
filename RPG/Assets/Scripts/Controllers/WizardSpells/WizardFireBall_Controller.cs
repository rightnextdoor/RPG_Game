using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardFireBall_Controller : Explosion
{
    private Animator anim;
    [SerializeField] private int damage;
    [SerializeField] private string targetLayerName = "Player";

    [SerializeField] private float xVelocity;
    [SerializeField] private Rigidbody2D rb;

    private float explosionTimer;
    
    private Player player;

    private void Update()
    {
        explosionTimer -= Time.deltaTime;

        if (explosionTimer < 0)
        {
            FinishExplosion();
        }

        if (canMove)
            rb.velocity = new Vector2(xVelocity, rb.velocity.y);
    }

    public void SetupFireBall(float _speed, CharacterStats _myStats, float _radius, float _explosionTimer)
    {
        anim = GetComponentInChildren<Animator>();

        xVelocity = _speed;
        myStats = _myStats;
        explosionRadius = _radius;
        explosionTimer = _explosionTimer;
        player = PlayerManager.instance.player;

        if (xVelocity > 0)
            transform.Rotate(0, 180, 0);

        canMove = true;
        anim.SetBool("Fired", true);
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
        anim.SetTrigger("Explode");
        canMove = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }
    public override void AnimationExplodeEvent()
    {
        isFire = true;
        base.AnimationExplodeEvent();      
        //AudioManager.instance.PlaySFX("BubbleImpact", transform);
    }

    private void FinishExplosion()
    {
        canMove = false;
        anim.SetTrigger("Explode");
    }
}
