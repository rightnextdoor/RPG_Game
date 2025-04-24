using UnityEngine;

public class UI_Buttons : MonoBehaviour
{
    public void RestartGame()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.RestartScene();
            GameManager.instance?.PauseGame(false);
        }
        else
        {
            Debug.LogWarning("GameManager instance not found. Cannot restart scene.");
        }
    }

    public void QuitGame()
    {
        // TODO: Add logic for quitting the game
        Debug.Log("QuitGame called — logic not yet implemented.");
    }

    public void GoToMainMenu()
    {
        // TODO: Add logic for returning to the main menu
        Debug.Log("GoToMainMenu called — logic not yet implemented.");
    }
}
