using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardProjectile_Controller : Explosion
{
    private Vector3 xVelocity;
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
            rb.velocity = xVelocity;
    }

    public void SetupProjectile(float _speed, CharacterStats _myStats, float _radius, float _explosionTimer, Vector3 _playerPos, Vector3 _dir)
    {
        anim = GetComponentInChildren<Animator>();

        //xVelocity = _speed;
        myStats = _myStats;
        explosionRadius = _radius;
        explosionTimer = _explosionTimer;
        player = PlayerManager.instance.player;
        
        Vector3 direction = (_playerPos - transform.position).normalized;
        if (_dir.x < 1 && _dir.x > -1)
        {
            direction.y *= -1;
        }

        if (_playerPos.x < transform.position.x)
        {
            xVelocity = direction * _speed * -1;
        }
        else
        {
            xVelocity = direction * _speed * 1;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);
        
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
        anim.SetTrigger("Explode");
        canMove = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }
    public override void AnimationExplodeEvent()
    {
        base.AnimationExplodeEvent();
    }

    private void FinishExplosion()
    {
        canMove = false;
        anim.SetTrigger("Explode");
    }
}
