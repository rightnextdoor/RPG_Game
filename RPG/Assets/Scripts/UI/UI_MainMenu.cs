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

        string scene = CheckpointManager.instance.GetLastSaveScene();

        CheckpointManager.instance.ContinueGame();

        if(scene != null)
            fadeScreen.MainMenuFadTo(scene);
        else
            fadeScreen.MainMenuFadTo(sceneToLoad);


    }

    public void NewGame()
    {
        SaveManager.instance.DeleteSavedData();
        fadeScreen.MainMenuFadTo(sceneToLoad);
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
    }
}
