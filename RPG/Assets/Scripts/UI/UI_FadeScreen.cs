using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_FadeScreen : MonoBehaviour
{
    public static UI_FadeScreen instance;

    [SerializeField] private Image img;
    [SerializeField] private AnimationCurve curve;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void RespawnFade()
    {
        StartCoroutine(RespawnFadeOut());       
    }

    public void ReSpawnFadeIn()
    {
        StartCoroutine(FadeIn());
    }

    public void MainMenuFadTo(string scene)
    {
        StartCoroutine(FadeOut(scene));
    }

    public void FadeTo(string scene)
    {
        PlayerManager.instance.player.GetComponent<PlayerStats>().keepPlayerHealthSceneChange = true; //to keep current health when changing scenes
        SaveManager.instance.SaveGame();
        StartCoroutine(FadeOut(scene));
    }

    //public void TravelTo(string scene)
    //{
    //    PlayerManager.instance.player.GetComponent<PlayerStats>().keepPlayerHealthSceneChange = true;
    //    SaveManager.instance.SaveGame();
    //    StartCoroutine(FadeOutFast(scene));
    //}

    private IEnumerator FadeIn()
    {
        float t = 1.2f;

        while (t > 0f)
        {

            t -= Time.deltaTime;
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);
            yield return 0;
        }
    }

    private IEnumerator RespawnFadeOut()
    {
        float t = .9f;

        while (t < 1f)
        {

            t += Time.deltaTime;
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);
            yield return 0;
        }
    }

    private IEnumerator FadeOutFast(string scene)
    {
        float t = .9f;

        while (t < 1f)
        {

            t += Time.deltaTime;
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);
            yield return 0;
        }

        SceneManager.LoadScene(scene);
    }
    private IEnumerator FadeOut(string scene)
    {
        float t = 0f;

        while (t < 1f)
        {

            t += Time.deltaTime;
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);
            yield return 0;
        }

        SceneManager.LoadScene(scene);
    }
}
