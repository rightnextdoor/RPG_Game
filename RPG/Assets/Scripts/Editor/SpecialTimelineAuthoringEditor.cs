#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimEvent = UnityEngine.AnimationEvent;

[CustomEditor(typeof(SpecialTimelineAuthoring))]
public class SpecialTimelineAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var t = (SpecialTimelineAuthoring)target;

        var control = t.GetComponentInParent<SpecialAttackControl>(true);
        if (!control)
        {
            EditorGUILayout.HelpBox(
                "No SpecialAttackControl found. Put this component on the special prefab root or a child under it.",
                MessageType.Error);
            return;
        }

        if (GUILayout.Button("Insert Explode"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                string pointName = GetNextPointName(clip, "explodePoint");
                AddEventAtPlayhead(nameof(Special_AnimationTriggers.ExplosionPointTrigger), pointName);
            }
        }

        if (GUILayout.Button("Insert Sound"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                string pointName = GetNextPointName(clip, "soundPoint");
                AddEventAtPlayhead(nameof(Special_AnimationTriggers.SpecialSoundPointTrigger), pointName);
            }
        }

        if (GUILayout.Button("Insert SelfDestroy"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                AddEventAtPlayhead("SelfDestroy");
            }
        }

        if (GUILayout.Button("Remove Explode"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                RemoveLastPoint(clip, "explodePoint");
            }
        }

        if (GUILayout.Button("Remove Sound"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                RemoveLastPoint(clip, "soundPoint");
            }
        }

        if (GUILayout.Button("Remove SelfDestroy"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                RemoveLastSelfDestroy(clip);
            }
        }
    }

    private static void AddEventAtPlayhead(string method, string payload = "")
    {
        var clip = AnimationWindowUtil.GetActiveClip();
        if (clip == null)
        {
            Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            return;
        }

        float time = AnimationWindowUtil.GetCurrentTime(clip);
        var ev = new AnimEvent
        {
            time = Mathf.Clamp(time, 0f, clip.length),
            functionName = method,
            stringParameter = payload ?? string.Empty
        };

        var list = AnimationUtility.GetAnimationEvents(clip).ToList();
        list.Add(ev);
        AnimationUtility.SetAnimationEvents(clip, list.ToArray());
        EditorUtility.SetDirty(clip);
    }

    private static string GetNextPointName(AnimationClip clip, string prefix)
    {
        var events = AnimationUtility.GetAnimationEvents(clip);
        int max = 0;

        foreach (var ev in events)
        {
            if (string.IsNullOrEmpty(ev.stringParameter))
                continue;

            string pointName = ev.stringParameter;
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (int.TryParse(numberText, out int n))
                max = Mathf.Max(max, n);
        }

        return $"{prefix}{max + 1}";
    }

    private static int GetHighestPointNumber(AnimationClip clip, string prefix)
    {
        var events = AnimationUtility.GetAnimationEvents(clip);
        int max = 0;

        foreach (var ev in events)
        {
            if (string.IsNullOrEmpty(ev.stringParameter))
                continue;

            string pointName = ev.stringParameter;
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (int.TryParse(numberText, out int n))
                max = Mathf.Max(max, n);
        }

        return max;
    }

    private static void RemoveLastPoint(AnimationClip clip, string prefix)
    {
        var list = AnimationUtility.GetAnimationEvents(clip).ToList();

        int removeIndex = -1;
        int removePoint = -1;

        for (int i = 0; i < list.Count; i++)
        {
            var ev = list[i];
            if (string.IsNullOrEmpty(ev.stringParameter))
                continue;

            string pointName = ev.stringParameter;
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (int.TryParse(numberText, out int n) && n >= removePoint)
            {
                removePoint = n;
                removeIndex = i;
            }
        }

        if (removeIndex < 0 || removePoint < 0)
            return;

        list.RemoveAt(removeIndex);

        for (int i = 0; i < list.Count; i++)
        {
            var ev = list[i];
            if (string.IsNullOrEmpty(ev.stringParameter))
                continue;

            string pointName = ev.stringParameter;
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (!int.TryParse(numberText, out int n))
                continue;

            if (n > removePoint)
            {
                ev.stringParameter = $"{prefix}{n - 1}";
                list[i] = ev;
            }
        }

        AnimationUtility.SetAnimationEvents(clip, list.ToArray());
        EditorUtility.SetDirty(clip);
    }

    private static void RemoveLastSelfDestroy(AnimationClip clip)
    {
        var list = AnimationUtility.GetAnimationEvents(clip).ToList();

        int removeIndex = -1;
        float removeTime = -1f;

        for (int i = 0; i < list.Count; i++)
        {
            var ev = list[i];
            if (ev.functionName != "SelfDestroy")
                continue;

            if (ev.time >= removeTime)
            {
                removeTime = ev.time;
                removeIndex = i;
            }
        }

        if (removeIndex < 0)
            return;

        list.RemoveAt(removeIndex);
        AnimationUtility.SetAnimationEvents(clip, list.ToArray());
        EditorUtility.SetDirty(clip);
    }
}
#endif