using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Arrow_Controller : MonoBehaviour
{
    [SerializeField] private string targetLayerName = "Player";
    [SerializeField] private Rigidbody2D rb;

    private Vector3 xVelocity;

    private bool canMove;
    private bool flipped;

    private CharacterStats myStats;

    private void Update()
    {
        if (canMove)
            rb.velocity = xVelocity;
    }

    public void SetupArrow(float _speed, CharacterStats _myStats, Vector3 _playerPos, Vector3 dir)
    {
        myStats = _myStats;
        Vector3 direction = (_playerPos - transform.position).normalized;
        canMove = true;
        
        if (dir.x < 1 && dir.x > -1)
        {
            //if the player is under or over the enemy
            //fix the direction of the arrow
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
        
        transform.rotation = Quaternion.Euler(0,0,angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(targetLayerName))
        {
            myStats.DoDamage(collision.GetComponent<CharacterStats>());
            StuckInto(collision);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (collision.gameObject.tag == "Platform")
                return;
            StuckInto(collision);
        }
    }

    private void StuckInto(Collider2D collision)
    {
        GetComponentInChildren<ParticleSystem>().Stop();
        GetComponent<CapsuleCollider2D>().enabled = false;
        canMove = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        transform.parent = collision.transform;

        if (myStats.isDead)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, Random.Range(5, 7));
    }

    public void FlipArrow()
    {
        if (flipped)
            return;

        xVelocity.x = xVelocity.x * -1;
        xVelocity.y = xVelocity.y * -1;
        flipped = true;
        transform.Rotate(0, 180, 0);
        targetLayerName = "Enemy";
    }
}
