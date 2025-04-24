using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static event System.Action OnSceneLoaded;

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            OnSceneLoaded?.Invoke();
        };
    }
}
