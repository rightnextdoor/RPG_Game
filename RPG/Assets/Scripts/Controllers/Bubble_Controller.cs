using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble_Controller : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private int damage;
    [SerializeField] private string targetLayerName = "Player";

    private Vector3 xVelocity;
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private bool canMove;

    private CharacterStats myStats;
    private float explosionTimer;
    private float explosionRadius;

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

    public void SetupBubble(float _speed, CharacterStats _myStats, float _radius, float _explosionTimer, Vector3 _playerPos)
    {
        anim = GetComponent<Animator>();

        Vector3 direction = (_playerPos - transform.position).normalized;

        myStats = _myStats;
        explosionRadius = _radius;
        explosionTimer = _explosionTimer;
        canMove = true;

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

    private void AnimationExplodeEvent()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        AudioManager.instance.PlaySFX("BubbleImpact", transform);
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
        canMove = false;
        anim.SetTrigger("Explode");
    }
    private void SelfDestroy() => Destroy(gameObject);
}
