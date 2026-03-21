using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyTimelineAuthoring))]
public class Enemy_AnimationTriggers : MonoBehaviour
{
    private Enemy enemy => GetComponentInParent<Enemy>();

    private void AnimationTrigger() => enemy.AnimationFinishTrigger();

    public void AttackTrigger(string pointName)
    {
        if (string.IsNullOrWhiteSpace(pointName))
            return;

        enemy.AttackTrigger(pointName);
    }

    public void SoundTrigger(string pointName)
    {
        if (string.IsNullOrWhiteSpace(pointName))
            return;

        enemy.SoundTrigger(pointName);
    }
    private void SpeicalAttackTrigger() => enemy.AnimationSpecialAttackTrigger();
    
    private void SelfDestroy() => enemy.SelfDestroy();

    private void OpenCounterWindow() => enemy.OpenCounterAttackWindow();
    private void CloseCounterWindow() => enemy.CloseCounterAttackWindow();

}
