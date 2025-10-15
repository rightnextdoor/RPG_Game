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
    [HideInInspector] public int lastAttackIndex = -1;
    [HideInInspector] public int lastCheckIndex = -1;
#endif
}
