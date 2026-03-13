using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTimelineAuthoring : MonoBehaviour
{
    [Tooltip("Profile for this enemy type (drives Animation window dropdowns).")]
    public EnemyAttackProfile profile;

    // Optional: convenience focus of a selected check while authoring
    [HideInInspector] public string selectedAttack;
    [HideInInspector] public string selectedCheck;

#if UNITY_EDITOR
    [Header("Animation Preview Audio")]
    [Tooltip("When enabled, SOUND events on the active Animation Window clip preview in the editor while the clip is playing.")]
    public bool previewClipAudio = true;

    [HideInInspector] public int lastAttackIndex = -1;
    [HideInInspector] public int lastCheckIndex = -1;
#endif
}