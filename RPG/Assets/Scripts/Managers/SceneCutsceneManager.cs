using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class SceneCutsceneManager : MonoBehaviour
{
    [SerializeField] private GameObject cutscenePlayerPrefab;
    [SerializeField] private CutsceneLibrary cutsceneLibrary; 

    private LevelChanger activeChanger;
    private GameObject cutscenePlayer;
    private PlayableDirector director;
    private CutsceneType currentEntryType;

    private void Start()
    {
        Player player = PlayerManager.instance.player;

        //If skipping cutscene, just activate the real player
        if (GameManager.instance != null && GameManager.instance.skipEntryCutscene)
        {
            Debug.Log("game manager is called " + GameManager.instance.skipEntryCutscene);
            GameManager.instance.skipEntryCutscene = false;
            player.gameObject.SetActive(true);
            player.EnableControl();
            return;
        }

        player.gameObject.SetActive(false);
        FindActiveLevelChanger();

        if (activeChanger == null)
        {
            player.gameObject.SetActive(true);
            player.EnableControl();
            return;
        }

        Transform spawnPoint = activeChanger.GetSpawnPoint();
        string sceneName = SceneManager.GetActiveScene().name;
        currentEntryType = LevelConnection.ActiveConnection.GetEntryCutsceneType(sceneName);

        if (CameraZoneManager.instance != null)
        {
            CameraZoneManager.instance.PrepareForCutscene(spawnPoint.position);
        }

        cutscenePlayer = Instantiate(cutscenePlayerPrefab, spawnPoint.position, Quaternion.identity);
        SetCutsceneIdle(cutscenePlayer);

        UI_FadeScreen.instance.FadeIn(() =>
        {
            TimelineAsset cutscene = cutsceneLibrary.GetCutscene(currentEntryType);
            if (cutscene != null)
            {
                PlayCutsceneTimeline(cutscene);
            }
            else
            {
                player.transform.position = spawnPoint.position;
                player.gameObject.SetActive(true);
                player.EnableControl();
            }
        });
    }

    private void FindActiveLevelChanger()
    {
        foreach (var changer in FindObjectsOfType<LevelChanger>())
        {
            if (changer.GetLevelConnection() == LevelConnection.ActiveConnection)
            {
                activeChanger = changer;
                break;
            }
        }
    }

    private void SetCutsceneIdle(GameObject player)
    {
        var anim = player.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetBool("Idle", true);
        }
    }

    private void PlayCutsceneTimeline(TimelineAsset timeline)
    {
        director = gameObject.AddComponent<PlayableDirector>();
        director.playableAsset = timeline;

        foreach (var output in timeline.outputs)
        {
            if (output.streamName.Contains("Animation") || output.streamName.Contains("Animator"))
            {
                director.SetGenericBinding(output.sourceObject, cutscenePlayer.GetComponent<Animator>());
                ApplyFacingForType(cutscenePlayer, currentEntryType);
            }
        }

        //Force snap camera again to ensure it starts correctly
        if (CameraZoneManager.instance != null)
        {
            CameraZoneManager.instance.PrepareForCutscene(cutscenePlayer.transform.position);
        }

        director.stopped += OnCutsceneFinished;
        director.Play();

        //Failsafe timeout if cutscene doesn't end normally
        StartCoroutine(FallbackCutsceneEnd());
    }

    private IEnumerator FallbackCutsceneEnd()
    {
        yield return new WaitForSeconds(5f); // timeline should always be < 5 seconds
        if (cutscenePlayer != null)
        {
            OnCutsceneFinished(director);
        }
    }


    private void OnCutsceneFinished(PlayableDirector d)
    {
        Vector3 finalPosition = cutscenePlayer.transform.position;
        Destroy(cutscenePlayer);

        Player player = PlayerManager.instance.player;
        player.transform.position = finalPosition;

        if (currentEntryType == CutsceneType.RunInFromRight || currentEntryType == CutsceneType.JumpInFromRight)
        {
            player.Flip();
        }

        player.gameObject.SetActive(true);
        player.EnableControl();

        d.stopped -= OnCutsceneFinished;
        d.playableAsset = null;
    }

    private void ActivateRealPlayerAtSpawn()
    {
        Player player = PlayerManager.instance.player;
        player.transform.position = activeChanger.GetSpawnPoint().position;
        player.gameObject.SetActive(true);
        player.EnableControl();
    }

    public TimelineAsset GetExitCutscene(CutsceneType type)
    {
        return cutsceneLibrary.GetCutscene(type);
    }

    public GameObject GetCutscenePlayerPrefab()
    {
        return cutscenePlayerPrefab;
    }

    private void ApplyFacingForType(GameObject clone, CutsceneType type)
    {
        if (type == CutsceneType.RunInFromRight || type == CutsceneType.JumpInFromRight || type == CutsceneType.FallInFromRight)
        {
            clone.transform.rotation = Quaternion.Euler(0, 180f, 0);
        }
        else
        {
            clone.transform.rotation = Quaternion.identity;
        }
    }
}
