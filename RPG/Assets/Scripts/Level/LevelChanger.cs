// LevelChanger.cs (Updated: Fade to black immediately when cutscene starts)
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class LevelChanger : MonoBehaviour
{
    [SerializeField] private LevelConnection levelConnection;
    [SerializeField] private SceneField targetScene;
    [SerializeField] private Transform spawnPoint;

    public LevelConnection GetLevelConnection() => levelConnection;
    public Transform GetSpawnPoint() => spawnPoint;

    private void Start()
    {
        if (levelConnection == LevelConnection.ActiveConnection && !SceneLoadTracker.lastLoadedByCheckpoint)
        {
            Player player = PlayerManager.instance.player;
            if (player != null)
            {
                player.transform.position = spawnPoint.position;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            LevelConnection.ActiveConnection = levelConnection;

            SceneCutsceneManager manager = FindObjectOfType<SceneCutsceneManager>();
            string currentScene = SceneManager.GetActiveScene().name;
            CutsceneType exitType = levelConnection.GetExitCutsceneType(currentScene);
            TimelineAsset exitCutscene = manager.GetExitCutscene(exitType);
            GameObject cutscenePlayerPrefab = manager.GetCutscenePlayerPrefab();

            if (exitCutscene != null && cutscenePlayerPrefab != null)
            {
                PlayExitCutsceneWithClone(exitCutscene, cutscenePlayerPrefab, exitType);
            }
            else
            {
                UI_FadeScreen.instance.FadeOut(targetScene.SceneName);
            }
        }
    }

    private void PlayExitCutsceneWithClone(TimelineAsset cutscene, GameObject prefab, CutsceneType type)
    {
        Player player = PlayerManager.instance.player;
        Vector3 startPos = player.transform.position;

        player.DisableControl();
        player.gameObject.SetActive(false);

        GameObject clone = Instantiate(prefab, startPos, Quaternion.identity);
        ApplyFacingForType(clone, type);

        var anim = clone.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Move", true);
        }

        PlayableDirector director = gameObject.AddComponent<PlayableDirector>();
        director.playableAsset = cutscene;

        foreach (var output in cutscene.outputs)
        {
            if (output.streamName.Contains("Animation") || output.streamName.Contains("Animator"))
            {
                director.SetGenericBinding(output.sourceObject, clone.GetComponent<Animator>());
            }
        }

        UI_FadeScreen.instance.FadeToBlack(); // Immediately start fade to black

        director.stopped += (PlayableDirector d) =>
        {
            Destroy(clone);
            Destroy(d);
            StartCoroutine(LoadSceneAfterFade(targetScene.SceneName));
        };


        director.Play();
    }

    private IEnumerator LoadSceneAfterFade(string sceneName)
    {
        // Wait until the fade is fully opaque
        yield return new WaitUntil(() => UI_FadeScreen.instance.IsFullyBlack());

        SceneManager.LoadScene(sceneName);
    }


    private void ApplyFacingForType(GameObject clone, CutsceneType type)
    {
        if (type == CutsceneType.RunInFromRight || type == CutsceneType.JumpInFromRight)
        {
            clone.transform.rotation = Quaternion.Euler(0, 180f, 0);
        }
        else
        {
            clone.transform.rotation = Quaternion.identity;
        }
    }
}

public static class SceneLoadTracker
{
    public static bool lastLoadedByCheckpoint = false;
}

