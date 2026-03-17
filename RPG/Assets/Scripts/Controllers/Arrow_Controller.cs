using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Arrow_Controller : SpecialAttackControl
{
    [SerializeField] private string targetLayerName = "Player";
    [SerializeField] private Rigidbody2D rb;

    private Vector3 xVelocity;
    private bool canMove;
    private bool flipped;

    public override void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        base.Setup(_stats, _spawnSpecs);

        if (player == null)
            return;

        Vector3 playerPos = player.transform.position;
        Vector3 dir = playerPos - transform.position;
        Vector3 direction = dir.normalized;

        canMove = true;

        if (dir.x < 1 && dir.x > -1)
            direction.y *= -1;

        AttackSpawnSpec currentSpec = null;
        if (spawnSpecs != null && spawnSpecs.Count > 0)
            currentSpec = spawnSpecs[0];

        float speed = currentSpec != null ? currentSpec.speed : 0f;

        xVelocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected override void Update()
    {
        base.Update();

        if (canMove)
            rb.linearVelocity = xVelocity;
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
