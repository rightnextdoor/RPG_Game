using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpecialMovement))]
[RequireComponent(typeof(SpecialCollision))]
[RequireComponent(typeof(SpecialDamage))]
[RequireComponent(typeof(SpecialExplosion))]
public abstract class SpecialAttackControl : MonoBehaviour
{
    protected Player player;
    protected CharacterStats myStats;
    private Rigidbody2D rb;
    protected List<AttackSpawnSpec> spawnSpecs;

    [SerializeField] protected List<int> explodePoints = new();
    [SerializeField] protected StateSound[] sounds;

    [SerializeField] private SpecialMovement movement;
    [SerializeField] private SpecialCollision specialCollision;
    [SerializeField] private SpecialDamage specialDamage;
    [SerializeField] private SpecialExplosion specialExplosion;

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
        specialExplosion = GetComponent<SpecialExplosion>();
    }

    protected virtual void Update()
    {
    }

    #region Animation Trigger
    public virtual void ExplosionPointTrigger(string pointName)
    {
    }

    public virtual void SpecialSoundPointTrigger(string pointName)
    {
    }
    #endregion

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
    protected void CheckCollisionShape(SpecialCollisionShape collisionShape)
    {
        if (specialCollision == null) return;

        specialCollision.CheckCollisionShape(collisionShape);
    }

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

    protected Collider2D[] GetOverlapHits(SpecialCollisionShape explosionShape)
    {
        if (specialCollision == null)
            return new Collider2D[0];

        return specialCollision.GetOverlapHits(explosionShape);
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

    public void SelfDestroy()
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

    #region Explosion
    protected void Explosion(AttackSpawnSpec spec)
    {
        if (specialExplosion == null || spec == null) return;

        specialExplosion.Setup(spec);
    }

    protected void StartExplosion()
    {
        if (specialExplosion == null) return;

        specialExplosion.StartExplosion();
    }

    protected bool ExplosionStarted()
    {
        if (specialExplosion == null) return false;

        return specialExplosion.ExplosionStarted();
    }

    protected void FinishExplosion()
    {
        if (specialExplosion == null) return;

        specialExplosion.FinishExplosion();
    }

    protected bool ExplosionFinished()
    {
        if (specialExplosion == null) return false;

        return specialExplosion.ExplosionFinished();
    }
    #endregion
}