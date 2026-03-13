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

        EditorGUILayout.PropertyField(so.FindProperty("profile"));
        EditorGUILayout.Space(6);

        DrawPreviewSettings(so);

        var sceneEnemy = t.GetComponentInParent<Enemy>(true);
        var enemy = sceneEnemy ? sceneEnemy : (t.profile ? t.profile.GetEnemyPrototype() : null);

        if (!enemy)
        {
            EditorGUILayout.HelpBox(
                "No Enemy found. Put Enemy_Regular/Enemy_Boss on a parent of the Animator, or assign a Profile.",
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
            var spawns = attack.spawnPrefab ?? new List<AttackSpawnSpec>();
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
        DrawSelectedCheckInspector(sceneEnemy ? sceneEnemy : enemy, attack.name, idxCheck);

        if (check == null || check.checkTransform == null)
        {
            EditorGUILayout.HelpBox(
                "This check has no Transform. Create a child GameObject and assign it to 'Check Transform'.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Insert ATTACK at Playhead"))
        {
            var attackName = attackNames[idxAttack];
            var checkLabel = checkLabels[idxCheck];
            var spawnName = needsSpawn ? spawnNames[idxSpawn] : null;

            if (needsSpawn && spawnName != "(no spawn entries)")
                AddEventAtPlayhead(nameof(Enemy_AnimationTriggers.AttackTrigger), $"{attackName}|{checkLabel}|{spawnName}");
            else
                AddEventAtPlayhead(nameof(Enemy_AnimationTriggers.AttackTrigger), $"{attackName}|{checkLabel}");
        }

        using (new EditorGUI.DisabledScope(soundCount == 0))
        {
            if (GUILayout.Button("Insert SOUND at Playhead"))
                AddEventAtPlayhead(nameof(Enemy_AnimationTriggers.SoundTrigger), $"{attack.name}|{idxSound}");
        }

        if (GUILayout.Button("Insert RESOLVE at Playhead"))
            AddEventAtPlayhead("AnimationTrigger");

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
}
#endif