#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Use alias to avoid conflicts with any user-defined AnimationEvent MonoBehaviour.
using AnimEvent = UnityEngine.AnimationEvent;

[CustomEditor(typeof(EnemyTimelineAuthoring))]
public class EnemyTimelineAuthoringEditor : Editor
{
    private int idxAttack, idxCheck, idxSpawn, idxSound;

    public override void OnInspectorGUI()
    {
        var t = (EnemyTimelineAuthoring)target;
        var so = serializedObject;

        DrawPreviewSettings(so);

        var enemy = t.GetComponentInParent<Enemy>(true);

        if (!enemy)
        {
            EditorGUILayout.HelpBox(
                "No Enemy found. Put Enemy_Regular/Enemy_Boss on a parent of the Animator.",
                MessageType.Error);
            so.ApplyModifiedProperties();
            return;
        }

        var attacksList = enemy.attackDetails;
        if (attacksList == null || attacksList.Count == 0)
        {
            EditorGUILayout.HelpBox("This enemy has no AttackDetails.", MessageType.Warning);
            so.ApplyModifiedProperties();
            return;
        }

        var attacks = attacksList.Where(a => a != null && !string.IsNullOrEmpty(a.name)).ToList();
        var attackNames = attacks.Select(a => a.name).ToArray();
        idxAttack = Mathf.Clamp(idxAttack, 0, Mathf.Max(0, attackNames.Length - 1));
        idxAttack = EditorGUILayout.Popup("Attack", idxAttack, attackNames);
        var attack = attacks[idxAttack];

        var checks = attack.attackChecks ?? new List<AttackCheck>();
        var checkLabels = checks.Where(c => c != null && !string.IsNullOrEmpty(c.label)).Select(c => c.label).ToArray();
        if (checkLabels.Length == 0)
        {
            EditorGUILayout.HelpBox("Selected attack has no checks.", MessageType.Warning);
            so.ApplyModifiedProperties();
            return;
        }

        idxCheck = Mathf.Clamp(idxCheck, 0, Mathf.Max(0, checkLabels.Length - 1));
        idxCheck = EditorGUILayout.Popup("Check", idxCheck, checkLabels);
        var check = checks.First(c => c.label == checkLabels[idxCheck]);

        string[] spawnNames = System.Array.Empty<string>();
        bool needsSpawn = check.shape == AttackCheckShape.Point;
        if (needsSpawn)
        {
            var spawns = attack.spawnSpec ?? new List<AttackSpawnSpec>();
            spawnNames = spawns.Where(s => s != null && !string.IsNullOrEmpty(s.name)).Select(s => s.name).ToArray();
            if (spawnNames.Length == 0)
                spawnNames = new[] { "(no spawn entries)" };

            idxSpawn = Mathf.Clamp(idxSpawn, 0, spawnNames.Length - 1);
            idxSpawn = EditorGUILayout.Popup("Spawn (Point)", idxSpawn, spawnNames);
        }

        int soundCount = attack.sounds != null ? attack.sounds.Length : 0;
        var soundLabels = Enumerable.Range(0, soundCount).Select(i => $"[{i}]").ToArray();
        using (new EditorGUI.DisabledScope(soundCount == 0))
        {
            idxSound = Mathf.Clamp(idxSound, 0, Mathf.Max(0, soundCount - 1));
            idxSound = EditorGUILayout.Popup("Sound", idxSound, soundLabels);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Selected Check Settings", EditorStyles.boldLabel);
        DrawSelectedCheckInspector(enemy, attack.name, idxCheck);

        if (check == null || check.checkTransform == null)
        {
            EditorGUILayout.HelpBox(
                "This check has no Transform. Create a child GameObject and assign it to 'Check Transform'.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Selected Sound Settings", EditorStyles.boldLabel);
        DrawSelectedSoundInspector(enemy, attack.name, idxSound);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Insert Attack"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                string pointName = GetNextPointName(clip, "attackPoint");
                AddEventAtPlayhead(nameof(Enemy_AnimationTriggers.AttackTrigger), pointName);
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
                AddEventAtPlayhead(nameof(Enemy_AnimationTriggers.SoundTrigger), pointName);
            }
        }

        if (GUILayout.Button("Remove Attack"))
        {
            var clip = AnimationWindowUtil.GetActiveClip();
            if (clip == null)
            {
                Debug.LogWarning("No active Animation Clip selected in Animation Window.");
            }
            else
            {
                RemoveLastAttackPointAndReassign(clip, enemy);
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
                RemoveLastSoundPointAndReassign(clip, enemy);
            }
        }

        so.ApplyModifiedProperties();
    }

    private static void DrawPreviewSettings(SerializedObject so)
    {
        EditorGUILayout.LabelField("Animation Preview Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("previewClipAudio"));
        EditorGUILayout.HelpBox(
            "Editor-only preview. When the Animation Window clip is playing, authored SOUND events preview in the editor. Runtime game audio is unchanged.",
            MessageType.Info);
        EditorGUILayout.Space(8);
    }

    private void DrawSelectedCheckInspector(Enemy enemyInstance, string attackName, int checkIndex)
    {
        if (!enemyInstance) return;

        var enemySO = new SerializedObject(enemyInstance);
        var attacksProp = enemySO.FindProperty("attackDetails");
        if (attacksProp == null || !attacksProp.isArray) return;

        SerializedProperty foundAttack = null;
        for (int i = 0; i < attacksProp.arraySize; i++)
        {
            var el = attacksProp.GetArrayElementAtIndex(i);
            var nameProp = el.FindPropertyRelative("name");
            if (nameProp != null && nameProp.stringValue == attackName)
            {
                foundAttack = el;
                break;
            }
        }
        if (foundAttack == null) return;

        var checksProp = foundAttack.FindPropertyRelative("attackChecks");
        if (checksProp == null || !checksProp.isArray || checkIndex < 0 || checkIndex >= checksProp.arraySize) return;

        var checkProp = checksProp.GetArrayElementAtIndex(checkIndex);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(checkProp, includeChildren: true);
        EditorGUI.indentLevel--;

        enemySO.ApplyModifiedProperties();
    }

    private void DrawSelectedSoundInspector(Enemy enemyInstance, string attackName, int soundIndex)
    {
        if (!enemyInstance) return;

        var enemySO = new SerializedObject(enemyInstance);
        var attacksProp = enemySO.FindProperty("attackDetails");
        if (attacksProp == null || !attacksProp.isArray) return;

        SerializedProperty foundAttack = null;
        for (int i = 0; i < attacksProp.arraySize; i++)
        {
            var el = attacksProp.GetArrayElementAtIndex(i);
            var nameProp = el.FindPropertyRelative("name");
            if (nameProp != null && nameProp.stringValue == attackName)
            {
                foundAttack = el;
                break;
            }
        }
        if (foundAttack == null) return;

        var soundsProp = foundAttack.FindPropertyRelative("sounds");
        if (soundsProp == null || !soundsProp.isArray || soundIndex < 0 || soundIndex >= soundsProp.arraySize)
        {
            EditorGUILayout.HelpBox("Selected attack has no sounds.", MessageType.Info);
            return;
        }

        var soundProp = soundsProp.GetArrayElementAtIndex(soundIndex);

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(soundProp, includeChildren: true);
        EditorGUI.indentLevel--;

        enemySO.ApplyModifiedProperties();
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

            var parts = ev.stringParameter.Split('|');
            if (parts.Length == 0)
                continue;

            string pointName = parts[0];
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (int.TryParse(numberText, out int n))
                max = Mathf.Max(max, n);
        }

        return $"{prefix}{max + 1}";
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

            var parts = ev.stringParameter.Split('|');
            if (parts.Length == 0)
                continue;

            string pointName = parts[0];
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

            var parts = ev.stringParameter.Split('|');
            if (parts.Length == 0)
                continue;

            string pointName = parts[0];
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (!int.TryParse(numberText, out int n))
                continue;

            if (n > removePoint)
            {
                parts[0] = $"{prefix}{n - 1}";
                ev.stringParameter = string.Join("|", parts);
                list[i] = ev;
            }
        }

        AnimationUtility.SetAnimationEvents(clip, list.ToArray());
        EditorUtility.SetDirty(clip);
    }

    private static void RemoveLastAttackPointAndReassign(AnimationClip clip, Enemy enemy)
    {
        int removedPoint = GetHighestPointNumber(clip, "attackPoint");
        if (removedPoint < 1)
            return;

        RemoveLastPoint(clip, "attackPoint");
        ReassignAttackPointData(enemy, removedPoint);
    }

    private static void RemoveLastSoundPointAndReassign(AnimationClip clip, Enemy enemy)
    {
        int removedPoint = GetHighestPointNumber(clip, "soundPoint");
        if (removedPoint < 1)
            return;

        RemoveLastPoint(clip, "soundPoint");
        ReassignSoundPointData(enemy, removedPoint);
    }

    private static int GetHighestPointNumber(AnimationClip clip, string prefix)
    {
        var events = AnimationUtility.GetAnimationEvents(clip);
        int max = 0;

        foreach (var ev in events)
        {
            if (string.IsNullOrEmpty(ev.stringParameter))
                continue;

            var parts = ev.stringParameter.Split('|');
            if (parts.Length == 0)
                continue;

            string pointName = parts[0];
            if (!pointName.StartsWith(prefix))
                continue;

            string numberText = pointName.Substring(prefix.Length);
            if (int.TryParse(numberText, out int n))
                max = Mathf.Max(max, n);
        }

        return max;
    }

    private static void ReassignAttackPointData(Enemy enemy, int removedPoint)
    {
        if (enemy == null || enemy.attackDetails == null)
            return;

        var enemySO = new SerializedObject(enemy);
        var attackDetailsProp = enemySO.FindProperty("attackDetails");
        if (attackDetailsProp == null || !attackDetailsProp.isArray)
            return;

        for (int i = 0; i < attackDetailsProp.arraySize; i++)
        {
            var detailProp = attackDetailsProp.GetArrayElementAtIndex(i);
            UpdatePointListsInArray(detailProp.FindPropertyRelative("attackChecks"), "attackPoints", removedPoint);
            UpdatePointListsInArray(detailProp.FindPropertyRelative("spawnSpec"), "attackPoints", removedPoint);
        }

        enemySO.ApplyModifiedProperties();
        EditorUtility.SetDirty(enemy);
    }

    private static void ReassignSoundPointData(Enemy enemy, int removedPoint)
    {
        if (enemy == null || enemy.attackDetails == null)
            return;

        var enemySO = new SerializedObject(enemy);
        var attackDetailsProp = enemySO.FindProperty("attackDetails");
        if (attackDetailsProp == null || !attackDetailsProp.isArray)
            return;

        for (int i = 0; i < attackDetailsProp.arraySize; i++)
        {
            var detailProp = attackDetailsProp.GetArrayElementAtIndex(i);
            var soundsProp = detailProp.FindPropertyRelative("sounds");
            UpdatePointListsInArray(soundsProp, "soundPoints", removedPoint);
        }

        enemySO.ApplyModifiedProperties();
        EditorUtility.SetDirty(enemy);
    }

    private static void UpdatePointListsInArray(SerializedProperty arrayProp, string listName, int removedPoint)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var elementProp = arrayProp.GetArrayElementAtIndex(i);
            if (elementProp == null)
                continue;

            var pointsProp = elementProp.FindPropertyRelative(listName);
            if (pointsProp == null || !pointsProp.isArray)
                continue;

            for (int p = pointsProp.arraySize - 1; p >= 0; p--)
            {
                var pointProp = pointsProp.GetArrayElementAtIndex(p);
                int value = pointProp.intValue;

                if (value == removedPoint)
                {
                    pointsProp.DeleteArrayElementAtIndex(p);
                }
                else if (value > removedPoint)
                {
                    pointProp.intValue = value - 1;
                }
            }
        }
    }
}
#endif