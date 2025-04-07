using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class Tornado_Controller : Explosion
{
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private Transform check;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Rigidbody2D rb;
    private float moveSpeed;

    private void Update()
    {

        if (canMove)
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
    }

    public void SetupTornadoAttack(CharacterStats _myStats, float _moveSpeed)
    {
        anim = GetComponentInChildren<Animator>();
        myStats = _myStats;
        moveSpeed = _moveSpeed;
        canMove = true;
        AudioManager.instance.PlaySFX("TornadoAttack");

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
                    hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                    myStats.DoDamage(hit.GetComponent<CharacterStats>());
                }
            }
        }
    }

    private void OnDrawGizmos() => Gizmos.DrawWireCube(check.position, boxSize);
}
