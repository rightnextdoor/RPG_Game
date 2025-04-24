using UnityEngine;

public class UI_GameOver : MonoBehaviour
{
    [SerializeField] private GameObject gameOverUI;

    public void ShowGameOverScreen()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(true);
        else
            Debug.LogWarning("[UI_GameOver] GameOverUI is not assigned.");
    }
}
