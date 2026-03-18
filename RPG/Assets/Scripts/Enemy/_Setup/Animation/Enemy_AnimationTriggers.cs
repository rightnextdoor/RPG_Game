using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyTimelineAuthoring))]
public class Enemy_AnimationTriggers : MonoBehaviour
{
    private Enemy enemy => GetComponentInParent<Enemy>();

    private void AnimationTrigger() => enemy.AnimationFinishTrigger();

    public void AttackTrigger(string packed)
    {
        if (string.IsNullOrEmpty(packed)) return;

        var parts = packed.Split('|');
        if (parts.Length >= 2)
            enemy.AttackTrigger(parts[0], parts[1]);
    }
    public void SoundTrigger(string packed)
    {
        if (string.IsNullOrEmpty(packed)) return;
        var parts = packed.Split('|');
        if (parts.Length >= 2 && int.TryParse(parts[1], out var idx))
            enemy.SoundTrigger(parts[0], idx);
    }
    private void SpeicalAttackTrigger() => enemy.AnimationSpecialAttackTrigger();
    
    private void SelfDestroy() => enemy.SelfDestroy();

    private void OpenCounterWindow() => enemy.OpenCounterAttackWindow();
    private void CloseCounterWindow() => enemy.CloseCounterAttackWindow();

}
