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
            myStats.DoDamage(hitCollision.GetComponent<CharacterStats>());
            StuckInto();
            ClearHit();
        }
        else if (hitType == SpecialHitType.Ground)
        {
            StuckInto();
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