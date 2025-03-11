using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike_Controller : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    private Player player;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CharacterStats>() != null)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                collision.GetComponent<CharacterStats>().KillEntity();
            }

            if (collision.gameObject.tag == "Player")
            {
                player = PlayerManager.instance.player;
                player.stats.TakeDamage(20);
                player.StopPlayer();
                player.PlayerCanMove(false);
                StartCoroutine(DelayFade());
            }
        }
    }

    private void FadeScreen()
    {
        UI_FadeScreen.instance.RespawnFade();
        StartCoroutine(RespawnPlayer());
        UI_FadeScreen.instance.ReSpawnFadeIn();
        player.PlayerCanMove(true);

    }

    private IEnumerator DelayFade()
    {
        yield return new WaitForSeconds(.5f);
        FadeScreen();
    }

    private IEnumerator RespawnPlayer()
    {
        yield return new WaitForSeconds(.1f);
        player.ReturnPlayerMove();
        player.transform.position = spawnPoint.position;
    }
}
