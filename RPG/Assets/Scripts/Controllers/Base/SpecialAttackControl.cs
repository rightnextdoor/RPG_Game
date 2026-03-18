using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpecialMovement))]
[RequireComponent(typeof(SpecialCollision))]
[RequireComponent(typeof(SpecialDamage))]
public abstract class SpecialAttackControl : MonoBehaviour
{
    protected Player player;
    protected CharacterStats myStats;
    private Rigidbody2D rb;
    protected List<AttackSpawnSpec> spawnSpecs;

    [SerializeField] private SpecialMovement movement;
    [SerializeField] private SpecialCollision specialCollision;
    [SerializeField] private SpecialDamage specialDamage;

    public virtual void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        myStats = _stats;
        spawnSpecs = _spawnSpecs;
        player = PlayerManager.instance.player;
        rb = GetComponent<Rigidbody2D>();

        AssignCategories();
        specialCollision.Setup(rb);
        specialDamage.Setup(myStats);
    }

    private void AssignCategories()
    {
        movement = GetComponent<SpecialMovement>();
        specialCollision = GetComponent<SpecialCollision>();
        specialDamage = GetComponent<SpecialDamage>();
    }

    protected virtual void Update()
    {
    }

    #region Movement
    protected virtual void Movement(AttackSpawnSpec spec)
    {
        if (movement == null || spec == null) return;

        movement.Setup(spec, player);
    }

    protected void ChangeDirection()
    {
        if (movement == null) return;

        movement.ChangeDirection();
    }

    protected void StopMovement()
    {
        if (movement == null) return;

        movement.StopMovement();
    }
    #endregion

    #region Collision
    protected bool TryGetHit(out Collider2D hitCollision, out SpecialHitType hitType)
    {
        if (specialCollision == null)
        {
            hitCollision = null;
            hitType = SpecialHitType.None;
            return false;
        }

        return specialCollision.TryGetHit(out hitCollision, out hitType);
    }

    protected void ClearHit()
    {
        if (specialCollision == null) return;

        specialCollision.ClearHit();
    }

    protected void ChangeTargetLayer(string newTargetLayerName)
    {
        if (specialCollision == null) return;

        specialCollision.ChangeTargetLayer(newTargetLayerName);
    }

    protected void StuckInto()
    {
        if (movement != null)
            movement.StopMovement();

        if (specialCollision == null) return;

        specialCollision.StuckInto();
    }
    #endregion

    #region Damage
    protected void DoDamage(Collider2D[] targets)
    {
        if (specialDamage == null) return;

        specialDamage.DoDamage(targets);
    }

    protected void SelfDestroy()
    {
        if (specialDamage == null) return;

        specialDamage.SelfDestroy();
    }

    protected void DestroyAfter(float time, bool useOwnerDeath)
    {
        if (specialDamage == null) return;

        specialDamage.DestroyAfter(time, useOwnerDeath);
    }
    #endregion
}