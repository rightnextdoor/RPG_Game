using System.Collections;
using UnityEngine;

//example 1 Player player = PlayerUtils.GetPlayerSafe(); player.DoSomething();
//example 2 StartCoroutine(PlayerUtils.WaitForPlayer(player => { player.DoSomething(); }));
public static class PlayerUtils
{
    private static Player cachedPlayer;

    // Immediate fetch (if already ready), otherwise returns null
    public static Player GetPlayerSafe()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
        {
            cachedPlayer = PlayerManager.instance.player;
            return cachedPlayer;
        }

        return null;
    }

    // Coroutine-based: waits until player is available, then returns via callback
    public static IEnumerator WaitForPlayer(System.Action<Player> onReady)
    {
        yield return new WaitUntil(() =>
            PlayerManager.instance != null && PlayerManager.instance.player != null);

        cachedPlayer = PlayerManager.instance.player;
        onReady?.Invoke(cachedPlayer);
    }

    // Async/Task-based (optional if using async code in the future)
#if UNITY_EDITOR || UNITY_STANDALONE
    public static async System.Threading.Tasks.Task<Player> WaitForPlayerAsync()
    {
        while (PlayerManager.instance == null || PlayerManager.instance.player == null)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        cachedPlayer = PlayerManager.instance.player;
        return cachedPlayer;
    }
#endif
}
