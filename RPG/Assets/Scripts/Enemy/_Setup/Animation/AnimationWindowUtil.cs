#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
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

    public static bool IsPlaying()
    {
        var window = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault();
        if (window == null)
            return false;

        return TryReadBool(window, "playing")
            || TryReadBool(window, "isPlaying")
            || TryReadBool(window, "previewing")
            || TryReadBool(window, "isPreviewing")
            || TryReadBool(window, "recording");
    }

    private static bool TryReadBool(object target, string memberName)
    {
        if (target == null || string.IsNullOrEmpty(memberName))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = target.GetType();

        var prop = type.GetProperty(memberName, flags);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            try { return (bool)prop.GetValue(target); }
            catch { }
        }

        var field = type.GetField(memberName, flags);
        if (field != null && field.FieldType == typeof(bool))
        {
            try { return (bool)field.GetValue(target); }
            catch { }
        }

        var method = type.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
        if (method != null && method.ReturnType == typeof(bool))
        {
            try { return (bool)method.Invoke(target, null); }
            catch { }
        }

        return false;
    }
}
#endif