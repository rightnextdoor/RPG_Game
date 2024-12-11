using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private string sceneName = "MainScene";
    [SerializeField] private GameObject continueButton;
    [SerializeField] UI_FadeScreen fadeScreen;

    private void Start()
    {
        AudioManager.instance.PlayBGM("MenuTheme");

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
        string scene = CheckpointManager.instance.GetLastSaveScene();

        if(scene != null)
            sceneName = scene;

        fadeScreen.MainMenuFadTo(sceneName);
    }

    public void NewGame()
    {
        SaveManager.instance.DeleteSavedData();
        fadeScreen.MainMenuFadTo(sceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
    }
}
