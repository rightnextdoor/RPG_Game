using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Options : MonoBehaviour, ISaveManager
{
    [Header("Audio Settings")]
    [SerializeField] private UI_VolumeSlider[] volumeSettings;

    [Header("Scene Settings")]
    [SerializeField] private SceneField MainMenuScene;

    private void OnEnable()
    {
        // Wait 1 frame to ensure AudioManager + AudioSources are initialized
        StartCoroutine(ApplySavedVolumesNextFrame());
    }

    private IEnumerator ApplySavedVolumesNextFrame()
    {
        yield return null;

        foreach (var item in volumeSettings)
        {
            if (item != null)
                item.SliderValue(item.slider.value); // Re-apply saved values to AudioMixer
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
    }

    public void GoToMainMenu()
    {
        if (MainMenuScene != null && !string.IsNullOrEmpty(MainMenuScene.SceneName))
        {
            Debug.Log("Loading Main Menu scene: " + MainMenuScene.SceneName);
            SceneManager.LoadScene(MainMenuScene.SceneName);
        }
        else
        {
            Debug.LogWarning("MainMenuScene not set in inspector.");
        }
    }

    public void OpenGameSettings()
    {
        Debug.Log("Opening Game Settings (not yet implemented)");
        // Placeholder for future custom settings logic
    }

    public void LoadData(GameData _data)
    {
        foreach (var pair in _data.volumeSettings)
        {
            foreach (var item in volumeSettings)
            {
                if (item != null && item.parameter == pair.Key)
                {
                    item.LoadSlider(pair.Value); // Load value onto slider
                }
            }
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.volumeSettings.Clear();

        foreach (var item in volumeSettings)
        {
            if (item != null && item.slider != null)
                _data.volumeSettings[item.parameter] = item.slider.value;
        }
    }
}
