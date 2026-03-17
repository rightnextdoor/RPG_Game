using System.Collections.Generic;
using UnityEngine;

public abstract class SpecialAttackControl : MonoBehaviour
{
    protected Player player;
    protected CharacterStats myStats;
    protected List<AttackSpawnSpec> spawnSpecs;

    public virtual void Setup(CharacterStats _stats, List<AttackSpawnSpec> _spawnSpecs)
    {
        myStats = _stats;
        spawnSpecs = _spawnSpecs;
        player = PlayerManager.instance.player;
    }

    protected virtual void Update()
    {
    }
}