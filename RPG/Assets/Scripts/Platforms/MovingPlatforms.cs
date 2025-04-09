using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatforms : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform startingPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float waitTime = 3f;
    private bool canMove;
    private bool playerOnPlatform;

    private int direction = -1;

    private void FixedUpdate()
    {
        if (canMove) 
            MoveToTarget();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            playerOnPlatform = true;
            player.gameObject.transform.parent = transform;
            if(!canMove)
                StartCoroutine(MovePlatorm());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            playerOnPlatform = false;
            player.gameObject.transform.parent = null;
            
            if (direction == 1)
            {
                Vector2 target = CurrentMovementTarget();
                float distance = (target - (Vector2)transform.position).magnitude;

                if (distance <= 0.1f)
                {
                    canMove = false;
                }
            }
        }
    }

    private IEnumerator MovePlatorm()
    {    
        yield return new WaitForSeconds(waitTime);

        canMove = true;
    }

    private void MoveToTarget()
    {
        Vector2 target = CurrentMovementTarget();

        transform.position = Vector2.Lerp(transform.position, target, moveSpeed * Time.deltaTime);

        float distance = (target - (Vector2)transform.position).magnitude;

        if (distance <= 0.1f)
        {
            direction *= -1;
            canMove = false;
            if (direction == -1 && !playerOnPlatform)
            {
                return;
            }
            StartCoroutine(MovePlatorm());
        }
    }

    private Vector3 CurrentMovementTarget()
    {
        if (direction == 1)
        {
            return startingPoint.position;
        } else
        {
            return endPoint.position;
        }
    }

    private void OnDrawGizmos()
    {
       if (startingPoint != null && endPoint != null)
        {
            Gizmos.DrawLine(transform.position, startingPoint.position);
            Gizmos.DrawLine(transform.position, endPoint.position);
        }
    }
}
