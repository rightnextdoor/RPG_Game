using UnityEngine;

public class UIManagerButtonHelper : MonoBehaviour
{
    //Called from Button OnClick in the Inspector
    public void RestartGame()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.RestartScene();
        }
        else
        {
            Debug.LogWarning("[UIManagerButtonHelper] GameManager not found.");
        }
    }

    // Optional: add more helpers in the future!
    // public void GoToMainMenu() { ... }
    // public void QuitGame() { ... }
}
