#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AnimEvent = UnityEngine.AnimationEvent;

[InitializeOnLoad]
public static class EnemyTimelineSoundPreview
{
    private const float EVENT_TOUCH_EPSILON = 0.001f;
    private const float EVENT_REARM_DISTANCE = 0.02f;

    private static AnimationClip lastClip;
    private static float lastTime;
    private static bool hasPreviewState;
    private static readonly Dictionary<string, bool> eventLatched = new();
    private static int delayedPreviewToken;

    static EnemyTimelineSoundPreview()
    {
        EditorApplication.update += OnEditorUpdate;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreviewAudio;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        StopPreviewAudio();
        ResetState();
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var authoring = FindActiveAuthoring();
        if (authoring == null || !authoring.previewClipAudio)
        {
            StopPreviewAudio();
            ResetState();
            return;
        }

        var clip = AnimationWindowUtil.GetActiveClip();
        if (clip == null || clip.length <= 0f)
        {
            StopPreviewAudio();
            ResetState();
            return;
        }

        float currentTime = Mathf.Clamp(AnimationWindowUtil.GetCurrentTime(clip), 0f, clip.length);

        if (!hasPreviewState || clip != lastClip)
        {
            StopPreviewAudio();
            lastClip = clip;
            lastTime = currentTime;
            hasPreviewState = true;
            eventLatched.Clear();
            delayedPreviewToken++;
            return;
        }

        PreviewTouchedSoundEvents(authoring, clip, lastTime, currentTime);
        RearmEvents(clip, currentTime);
        lastTime = currentTime;
    }

    private static void PreviewTouchedSoundEvents(EnemyTimelineAuthoring authoring, AnimationClip clip, float previousTime, float currentTime)
    {
        AnimEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        if (events == null || events.Length == 0)
            return;

        foreach (AnimEvent ev in events)
        {
            if (ev == null)
                continue;

            if (!string.Equals(ev.functionName, nameof(Enemy_AnimationTriggers.SoundTrigger), StringComparison.Ordinal))
                continue;

            string key = BuildEventKey(ev);
            if (IsLatched(key))
                continue;

            if (!DidTouchEvent(previousTime, currentTime, ev.time, clip.length))
                continue;

            LatchEvent(key);
            PreviewPackedSound(authoring, ev.stringParameter);
        }
    }

    private static bool DidTouchEvent(float previousTime, float currentTime, float eventTime, float clipLength)
    {
        if (Mathf.Abs(currentTime - eventTime) <= EVENT_TOUCH_EPSILON)
            return true;

        if (Mathf.Abs(previousTime - eventTime) <= EVENT_TOUCH_EPSILON)
            return false;

        if (Mathf.Abs(currentTime - previousTime) <= EVENT_TOUCH_EPSILON)
            return false;

        bool wrapped = IsRealLoopWrap(previousTime, currentTime, clipLength);

        if (!wrapped)
        {
            float minTime = Mathf.Min(previousTime, currentTime);
            float maxTime = Mathf.Max(previousTime, currentTime);

            return eventTime > minTime + EVENT_TOUCH_EPSILON &&
                   eventTime <= maxTime + EVENT_TOUCH_EPSILON;
        }

        bool crossedTail = eventTime > previousTime + EVENT_TOUCH_EPSILON &&
                           eventTime <= clipLength + EVENT_TOUCH_EPSILON;

        bool crossedHead = eventTime >= -EVENT_TOUCH_EPSILON &&
                           eventTime <= currentTime + EVENT_TOUCH_EPSILON;

        return crossedTail || crossedHead;
    }

    private static bool IsRealLoopWrap(float previousTime, float currentTime, float clipLength)
    {
        if (clipLength <= 0f)
            return false;

        if (currentTime >= previousTime)
            return false;

        float endWindow = clipLength * 0.10f;
        float startWindow = clipLength * 0.10f;

        bool wasNearEnd = previousTime >= clipLength - endWindow;
        bool isNearStart = currentTime <= startWindow;
        bool bigJump = (previousTime - currentTime) >= clipLength * 0.50f;

        return wasNearEnd && isNearStart && bigJump;
    }

    private static void RearmEvents(AnimationClip clip, float currentTime)
    {
        AnimEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        if (events == null || events.Length == 0)
            return;

        foreach (AnimEvent ev in events)
        {
            if (ev == null)
                continue;

            if (!string.Equals(ev.functionName, nameof(Enemy_AnimationTriggers.SoundTrigger), StringComparison.Ordinal))
                continue;

            string key = BuildEventKey(ev);
            if (!IsLatched(key))
                continue;

            if (Mathf.Abs(currentTime - ev.time) > EVENT_REARM_DISTANCE)
                eventLatched[key] = false;
        }
    }

    private static string BuildEventKey(AnimEvent ev)
    {
        return $"{ev.functionName}|{ev.time:F6}|{ev.stringParameter}";
    }

    private static bool IsLatched(string key)
    {
        return eventLatched.TryGetValue(key, out bool value) && value;
    }

    private static void LatchEvent(string key)
    {
        eventLatched[key] = true;
    }

    private static void PreviewPackedSound(EnemyTimelineAuthoring authoring, string packed)
    {
        if (string.IsNullOrEmpty(packed))
            return;

        var parts = packed.Split('|');
        if (parts.Length < 2)
            return;

        if (!int.TryParse(parts[1], out int soundIndex))
            return;

        var enemy = ResolveAuthoringEnemy(authoring);
        if (enemy == null || enemy.attackDetails == null)
            return;

        var attack = enemy.attackDetails.FirstOrDefault(a => a != null && a.name == parts[0]);
        if (attack == null || attack.sounds == null)
            return;

        if (soundIndex < 0 || soundIndex >= attack.sounds.Length)
            return;

        var sound = attack.sounds[soundIndex];
        if (string.IsNullOrEmpty(sound.name))
            return;

        var clip = FindClipBySoundName(sound.name);
        if (clip == null)
        {
            Debug.LogWarning($"Timeline preview could not find clip for sound '{sound.name}'.");
            return;
        }

        float delay = Mathf.Max(0f, sound.delay);
        if (delay > 0f)
            StartDelayedPreview(clip, delay);
        else
            PlayPreviewClip(clip);
    }

    private static void StartDelayedPreview(AudioClip clip, float delay)
    {
        if (clip == null)
            return;

        int token = ++delayedPreviewToken;
        double triggerAt = EditorApplication.timeSinceStartup + delay;

        void WaitForDelay()
        {
            if (token != delayedPreviewToken)
            {
                EditorApplication.update -= WaitForDelay;
                return;
            }

            if (EditorApplication.timeSinceStartup < triggerAt)
                return;

            EditorApplication.update -= WaitForDelay;
            PlayPreviewClip(clip);
        }

        EditorApplication.update += WaitForDelay;
    }

    private static EnemyTimelineAuthoring FindActiveAuthoring()
    {
        var all = Resources.FindObjectsOfTypeAll<EnemyTimelineAuthoring>();
        if (all == null || all.Length == 0)
            return null;

        if (Selection.activeGameObject != null)
        {
            var selected = Selection.activeGameObject.GetComponentInParent<EnemyTimelineAuthoring>();
            if (selected != null)
                return selected;
        }

        return all[0];
    }

    private static Enemy ResolveAuthoringEnemy(EnemyTimelineAuthoring authoring)
    {
        if (authoring == null)
            return null;

        var sceneEnemy = authoring.GetComponentInParent<Enemy>(true);
        if (sceneEnemy != null)
            return sceneEnemy;

        return authoring.profile ? authoring.profile.GetEnemyPrototype() : null;
    }

    private static AudioClip FindClipBySoundName(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
            return null;

        string[] guids = AssetDatabase.FindAssets("t:SoundLibrary");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
            if (library == null || library.sounds == null)
                continue;

            var sound = library.sounds.FirstOrDefault(s => s != null && s.name == soundName);
            if (sound != null && sound.clip != null)
                return sound.clip;
        }

        return null;
    }

    private static void PlayPreviewClip(AudioClip clip)
    {
        if (clip == null)
            return;

        StopPreviewAudio();

        var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtil == null)
            return;

        var method =
            audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
            ?? audioUtil.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] { typeof(AudioClip) }, null);

        if (method == null)
            return;

        try
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 3)
                method.Invoke(null, new object[] { clip, 0, false });
            else
                method.Invoke(null, new object[] { clip });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void StopPreviewAudio()
    {
        delayedPreviewToken++;

        var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtil == null)
            return;

        var method =
            audioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? audioUtil.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (method == null)
            return;

        try
        {
            method.Invoke(null, null);
        }
        catch
        {
        }
    }

    private static void ResetState()
    {
        lastClip = null;
        lastTime = 0f;
        hasPreviewState = false;
        eventLatched.Clear();
        delayedPreviewToken++;
    }
}
#endif