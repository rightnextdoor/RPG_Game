using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyZone : MonoBehaviour
{
    [SerializeField] private LayerMask whatIsEnemy;
    private BoxCollider2D cd => GetComponent<BoxCollider2D>();
    private bool playerDetected;
    private int enemyCount = 0;

    public bool PlayerInZone => playerDetected;

    public int EnemyCount => enemyCount;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            playerDetected = true;
        }

        if (collision.GetComponent<Enemy_Regular>() != null)
        {
            enemyCount++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            playerDetected = false;
        }

        if (collision.GetComponent<Enemy_Regular>() != null)
        {
            enemyCount--;
        }
    }

    public void KillSpawnEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, cd.size, whatIsEnemy);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Enemy_Regular>() != null)
            {
                hit.GetComponent<Enemy_Regular>().SelfDestroy();
            }
        }
    }
}
