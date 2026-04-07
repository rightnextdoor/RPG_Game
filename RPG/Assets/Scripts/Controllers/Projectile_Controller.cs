using System.Collections.Generic;
using UnityEngine;

public class Projectile_Controller : SpecialAttackControl
{
    private bool flipped;
    private AttackSpawnSpec currentSpec;

    public override void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        base.Setup(_stats, _spawnSpecs);

        currentSpec = null;
        if (spawnSpecs != null && spawnSpecs.Count > 0)
            currentSpec = spawnSpecs[0];

        if (currentSpec != null)
            CheckCollisionShape(currentSpec.collisionShape);

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
            if(currentSpec.canStuckInto)
                StuckInto();
            DestroyAfter(Random.Range(5, 7), true);
            ClearHit();
        }
        else if (hitType == SpecialHitType.Ground)
        {
            if (currentSpec.canStuckInto)
                StuckInto();
            DestroyAfter(Random.Range(5, 7), true);
            ClearHit();
        }
    }

    public override bool FlipProjectile()
    {
        if(!currentSpec.canParry)
            return false;

        if (flipped)
            return false;

        ChangeDirection();
        ChangeTargetLayer("Enemy");

        flipped = true;
        return true;
    }
}
