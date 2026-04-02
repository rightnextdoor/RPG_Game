using System.Collections.Generic;
using UnityEngine;

public class Explosive_Controller : SpecialAttackControl
{
    private Animator anim;
    private AttackSpawnSpec currentSpec;

    public override void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        base.Setup(_stats, _spawnSpecs);

        anim = GetComponent<Animator>();

        currentSpec = null;
        if (spawnSpecs != null && spawnSpecs.Count > 0)
            currentSpec = spawnSpecs[0];

        if (currentSpec != null)
            CheckCollisionShape(currentSpec.collisionShape);

        Movement(currentSpec);
        Explosion(currentSpec);
    }

    protected override void Update()
    {
        base.Update();

        if (TryGetHit(out Collider2D hitCollision, out SpecialHitType hitType))
        {
            if (hitType == SpecialHitType.Target || hitType == SpecialHitType.Ground)
                StartExplosionFlow();
        }

        if (ExplosionStarted())
            StartExplosionFlow();

        if (ExplosionFinished())
            FinishExplosionFlow();
    }

    private void StartExplosionFlow()
    {
        StopMovement();
        StartExplosion();
        StartExplosionAnimation();
    }

    private void StartExplosionAnimation()
    {
        if (anim == null || currentSpec == null || !currentSpec.hasAnimation)
            return;

        if (string.IsNullOrWhiteSpace(currentSpec.animationBoolName))
            return;

        switch (currentSpec.animationType)
        {
            case SpecialAnimationType.Trigger:
                anim.SetTrigger(currentSpec.animationBoolName);
                break;

            case SpecialAnimationType.Bool:
                anim.SetBool(currentSpec.animationBoolName, true);
                break;
        }
    }

    private void FinishExplosionFlow()
    {
        if (currentSpec == null)
            return;

        Collider2D[] hits = GetOverlapHits(currentSpec.collisionShape);
        PlayFinishSounds();
        DoDamage(hits);
        DestroyAfter(currentSpec.destroyAfterTime, true);
    }

    private void PlayFinishSounds()
    {
        if (currentSpec == null || currentSpec.sounds == null || currentSpec.sounds.Length == 0)
            return;

        foreach (var sound in currentSpec.sounds)
        {
            if (sound.soundPoints != null && sound.soundPoints.Count > 0)
                continue;

            sound.Play(transform);
        }
    }

    public override void ExplosionPointTrigger(string pointName)
    {
        if (currentSpec == null || string.IsNullOrWhiteSpace(pointName))
            return;

        if (!pointName.StartsWith("explodePoint"))
            return;

        string numberText = pointName.Substring("explodePoint".Length);
        if (!int.TryParse(numberText, out int explodePoint))
            return;

        if (currentSpec.explodePoints == null || !currentSpec.explodePoints.Contains(explodePoint))
            return;

        FinishExplosion();
    }

    public override void SpecialSoundPointTrigger(string pointName)
    {
        if (currentSpec == null || currentSpec.sounds == null || string.IsNullOrWhiteSpace(pointName))
            return;

        if (!pointName.StartsWith("soundPoint"))
            return;

        string numberText = pointName.Substring("soundPoint".Length);
        if (!int.TryParse(numberText, out int soundPoint))
            return;

        foreach (var sound in currentSpec.sounds)
        {
            if (sound.soundPoints == null)
                continue;

            if (!sound.soundPoints.Contains(soundPoint))
                continue;

            sound.Play(transform);
        }
    }
}