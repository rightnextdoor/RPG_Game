using UnityEngine;

[RequireComponent(typeof(SpecialTimelineAuthoring))]
public class Special_AnimationTriggers : MonoBehaviour
{
    private SpecialAttackControl control => GetComponentInParent<SpecialAttackControl>();

    public void ExplosionPointTrigger(string pointName)
    {
        if (string.IsNullOrWhiteSpace(pointName) || control == null)
            return;

        control.ExplosionPointTrigger(pointName);
    }

    public void SpecialSoundPointTrigger(string pointName)
    {
        if (string.IsNullOrWhiteSpace(pointName) || control == null)
            return;

        control.SpecialSoundPointTrigger(pointName);
    }

    private void SelfDestroy()
    {
        if (control == null)
            return;

        control.SelfDestroy();
    }
}