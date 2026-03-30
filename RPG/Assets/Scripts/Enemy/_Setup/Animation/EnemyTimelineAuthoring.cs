using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTimelineAuthoring : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Animation Preview Audio")]
    [Tooltip("When enabled, SOUND events on the active Animation Window clip preview in the editor while the clip is playing.")]
    public bool previewClipAudio = true;
#endif
}