using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_FadeScreen : MonoBehaviour
{
    public static UI_FadeScreen instance;

    [SerializeField] private Image img;
    [SerializeField] private AnimationCurve curve;
    private bool isFading = false;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        FadeIn();
    }

    public void FadeIn(System.Action onCutsceneStart = null)
    {
        StartCoroutine(FadeInCoroutine(onCutsceneStart));
    }

    public void FadeOut(string sceneName = null, System.Action onFadeComplete = null)
    {
        if (isFading) return; //protect against double call
        isFading = true;

        PlayerManager.instance.player.GetComponent<PlayerStats>().keepPlayerHealthSceneChange = true;
        SaveManager.instance.SaveGame();
        StartCoroutine(FadeOutCoroutine(sceneName, onFadeComplete));
    }

    public bool IsFullyBlack()
    {
        return img.color.a >= 0.99f;
    }


    public void FadeToBlack(System.Action onComplete = null)
    {
        PlayerManager.instance.player.GetComponent<PlayerStats>().keepPlayerHealthSceneChange = true;
        SaveManager.instance.SaveGame();
        StartCoroutine(FadeOutCoroutine(null, onComplete));
    }

    private IEnumerator FadeInCoroutine(System.Action onCutsceneStart)
    {     
        float t = 1.2f;
        bool cutsceneStarted = false;

        while (t > 0f)
        {
            t -= Time.deltaTime;
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);

            // Begin cutscene near the start of the fade-in
            if (!cutsceneStarted && t < 1.0f)
            {
                cutsceneStarted = true;
                onCutsceneStart?.Invoke();
            }

            yield return null;
        }
    }

    private IEnumerator FadeOutCoroutine(string scene, System.Action onComplete)
    {
        Debug.Log("FadeOut started");
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime;
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        onComplete?.Invoke();

        if (!string.IsNullOrEmpty(scene))
        {
            yield return new WaitForEndOfFrame();
            SceneManager.LoadScene(scene);
        }

        isFading = false;
    }
}