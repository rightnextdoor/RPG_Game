using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpecialMovement))]
public abstract class SpecialAttackControl : MonoBehaviour
{
    protected Player player;
    protected CharacterStats myStats;
    protected List<AttackSpawnSpec> spawnSpecs;
    [SerializeField] private SpecialMovement movement;

    public virtual void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        myStats = _stats;
        spawnSpecs = _spawnSpecs;
        player = PlayerManager.instance.player;

        AssignCategories();
    }

    private void AssignCategories()
    {
        movement = GetComponent<SpecialMovement>();
    }

    protected virtual void Update()
    {
    }

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
}