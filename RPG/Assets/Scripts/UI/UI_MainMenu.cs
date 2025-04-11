using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private SceneField sceneToLoad;
    [SerializeField] private GameObject continueButton;
    [SerializeField] UI_FadeScreen fadeScreen;

    private void Start()
    {
        AudioManager.instance.PlayEventMusic("MenuTheme");

        StartCoroutine(LoadWithDelay());
    }

    private IEnumerator LoadWithDelay()
    {
        yield return new WaitForSeconds(.1f);

        if (SaveManager.instance.HasSavedData() == false)
            continueButton.SetActive(false);
    }

    public void ContinueGame()
    {
        EnemyManager.instance.ResetEnemyDeath();

        CheckpointManager.instance.ContinueGame();

        AudioManager.instance.StopEventMusicAndResumeRandom();
    }

    public void NewGame()
    {
        SaveManager.instance.DeleteSavedData();
        fadeScreen.FadeOut(sceneToLoad);
        Debug.Log("new game fadeout");
        AudioManager.instance.StopEventMusicAndResumeRandom();
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
    }
}
