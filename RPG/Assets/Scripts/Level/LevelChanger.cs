using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    [SerializeField] private LevelConnection levelConnection;
    [SerializeField] private UI_FadeScreen fadeScreen;

    [SerializeField] private string targetSceneName;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool isFacingRight;

    private void Start()
    {
        if (levelConnection == LevelConnection.ActiveConnection)
        {
            Player player = PlayerManager.instance.player;
            if (player != null)
            {
                if (CheckpointManager.instance.isTraveling)
                {
                    CheckpointManager.instance.isTraveling = false;
                } else
                {
                    player.transform.position = spawnPoint.position;
                }

                if (isFacingRight)
                    player.Flip();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player player = collision.collider.GetComponent<Player>();

        if (player != null)
        {
            LevelConnection.ActiveConnection = levelConnection;
            fadeScreen.FadeTo(targetSceneName);
            //SceneManager.LoadScene(targetSceneName);
        }
    }
}
