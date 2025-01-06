using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    [SerializeField] private LevelConnection levelConnection;

    [SerializeField] private SceneField targetScene;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool isFacingRight;
    [SerializeField] private bool transitionUp;
    [SerializeField] private bool transitionDown;

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

                PlayerManager.instance.LevelTranstion(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            player.LevelTransition(transitionDown, transitionUp);
            PlayerManager.instance.LevelTranstion(true);
            LevelConnection.ActiveConnection = levelConnection;
            UI_FadeScreen.instance.FadeTo(targetScene);
        }
    }
}
