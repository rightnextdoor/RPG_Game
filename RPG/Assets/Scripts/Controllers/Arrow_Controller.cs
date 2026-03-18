using System.Collections.Generic;
using UnityEngine;

public class Arrow_Controller : SpecialAttackControl
{
    private bool flipped;

    public override void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        base.Setup(_stats, _spawnSpecs);

        AttackSpawnSpec currentSpec = null;
        if (spawnSpecs != null && spawnSpecs.Count > 0)
            currentSpec = spawnSpecs[0];

        Movement(currentSpec);
    }

    protected override void Update()
    {
        base.Update();

        if (!TryGetHit(out Collider2D hitCollision, out SpecialHitType hitType))
            return;

        if (hitType == SpecialHitType.Target)
        {
            DoDamage(new Collider2D[] { hitCollision });
            StuckInto();
            DestroyAfter(Random.Range(5, 7), true);
            ClearHit();
        }
        else if (hitType == SpecialHitType.Ground)
        {
            StuckInto();
            DestroyAfter(Random.Range(5, 7), true);
            ClearHit();
        }
    }

    public void FlipArrow()
    {
        if (flipped)
            return;

        ChangeDirection();
        ChangeTargetLayer("Enemy");

        flipped = true;
    }
}