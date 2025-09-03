#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AnimationWindowUtil
{
    public static AnimationClip GetActiveClip()
    {
        var window = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault();
        return window?.animationClip;
    }

    public static float GetCurrentTime(AnimationClip clip)
    {
        var window = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault();
        return window != null ? (float)window.time : 0f;
    }
}
#endif
