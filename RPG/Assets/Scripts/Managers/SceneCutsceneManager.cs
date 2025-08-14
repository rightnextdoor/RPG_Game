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

    private bool cutsceneEnded;
    private Coroutine fallbackRoutine;

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

        cutscenePlayer = Instantiate(cutscenePlayerPrefab, spawnPoint.position, Quaternion.identity);
        SetCutsceneIdle(cutscenePlayer);

        if (CameraZoneManager.instance != null)
        {
            CameraZoneManager.instance.SetCutsceneSubject(cutscenePlayer.transform);
            CameraZoneManager.instance.PrepareForCutscene(spawnPoint.position);  // <- hard cut & immediate rebuild
        }

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

        director.stopped += OnCutsceneFinished;
        director.Play();

        //Failsafe timeout if cutscene doesn't end normally
        fallbackRoutine = StartCoroutine(FallbackCutsceneEnd());

        if (fallbackRoutine != null) { StopCoroutine(fallbackRoutine); fallbackRoutine = null; }
        if (Application.isPlaying && isActiveAndEnabled && gameObject.activeInHierarchy)
            fallbackRoutine = StartCoroutine(FallbackCutsceneEnd());

    }

    private IEnumerator FallbackCutsceneEnd()
    {
        yield return new WaitForSeconds(10f); // timeline should always be < 5 seconds
        if (cutsceneEnded) yield break;

        if (Application.isPlaying && isActiveAndEnabled && gameObject.activeInHierarchy && director != null)
            OnCutsceneFinished(director);
    }


    private void OnCutsceneFinished(PlayableDirector d)
    {
        // If we’re exiting play mode or object is inactive, do minimal cleanup and bail
        if (!Application.isPlaying || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            if (d != null) d.stopped -= OnCutsceneFinished;
            if (d != null) Destroy(d);
            return;
        }

        if (cutsceneEnded) return;
        cutsceneEnded = true;

        // Stop fallback if still pending
        if (fallbackRoutine != null) { StopCoroutine(fallbackRoutine); fallbackRoutine = null; }

        // Unhook first to avoid re-entry
        if (d != null) d.stopped -= OnCutsceneFinished;

        // Cache final position BEFORE destroying anything
        Vector3 finalPosition = (cutscenePlayer != null)
            ? cutscenePlayer.transform.position
            : PlayerManager.instance.player.transform.position;

        // Cameras: stop treating clone as subject
        CameraZoneManager.instance?.ClearCutsceneSubject();

        // Move/enable real player
        var player = PlayerManager.instance.player;
        if (player != null)
        {
            player.transform.position = finalPosition;

            var cz = CameraZoneManager.instance?.CurrentZone;
            if (cz == null || cz.mode == CameraZone.ZoneMode.FreeFollow)
                CameraZoneManager.instance?.ResetToFreeFollow();

            player.gameObject.SetActive(true);
            player.EnableControl();
        }

        // Destroy clone safely
        if (cutscenePlayer != null)
        {
            var clone = cutscenePlayer;
            cutscenePlayer = null;
            Destroy(clone);
        }

        // Clean up director (use coroutine only if still active)
        if (d != null)
        {
            d.playableAsset = null;
            if (Application.isPlaying && isActiveAndEnabled && gameObject.activeInHierarchy)
                StartCoroutine(DestroyDirectorEndOfFrame(d));
            else
                Destroy(d);
        }
    }

    private void OnDisable()
    {
        if (director != null) director.stopped -= OnCutsceneFinished;
        if (fallbackRoutine != null) { StopCoroutine(fallbackRoutine); fallbackRoutine = null; }
        StopAllCoroutines();
    }


    private IEnumerator DestroyDirectorEndOfFrame(PlayableDirector dir)
    {
        yield return null; // wait one frame
        if (dir != null) Destroy(dir);
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
