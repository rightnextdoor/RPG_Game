using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platforms : MonoBehaviour
{
    private new Collider2D collider;
    private bool playerOnPlatform;

    private void Start()
    {
        collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (playerOnPlatform && Input.GetAxisRaw("Vertical") < 0)
        {
            collider.enabled = false;
            StartCoroutine(EnableCollider());
        }
    }

    private IEnumerator EnableCollider()
    {
        yield return new WaitForSeconds(0.5f);
        collider.enabled = true;
    }

    private void SetPlayerOnPlatform(Collision2D other, bool value)
    {
        var player = other.gameObject.GetComponent<Player>();
        if (player != null)
        {
            playerOnPlatform = value;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerManager.instance.player.canWallSlide = false;
        SetPlayerOnPlatform(collision, true);  
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerManager.instance.player.canWallSlide = true;
        SetPlayerOnPlatform(collision, true);
    }
}
