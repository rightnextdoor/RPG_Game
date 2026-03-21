using System.Collections.Generic;
using UnityEngine;

public enum SpecialMovementType
{
    None,
    Straight,
    Arch,
    Homing,
    Dive
}

[System.Serializable]
public class AttackSpawnSpec
{
    #region Prefab
    
    [Header("Prefab Basics")]
    [Tooltip("The name of the prefab to spawn.")]
    public string name;

    public List<int> attackPoints = new();

    [Tooltip("The projectile or special attack prefab to spawn.")]
    public GameObject prefab;
    
    [Tooltip("If true, the prefab will only spawn once per ON event (OFF resets it).")]
    public bool spawnOncePerOn = true;
    
    [Tooltip("Optional SFX cue name to play when this prefab is spawned.")]
    public string sfxOnSpawn;
    #endregion

    #region Control

    [Space(2)]
    [Header("Control")]
    [Tooltip("The special attack controller used to run this attack.")]
    public SpecialAttackControl control;

    [Space(2)]
    [Header("Movement")]
    [Tooltip("Initial speed for the projectile (usually multiplied by facingDir).")]
    public float speed = 5f;

    #region Homing
    [Tooltip("How this spawned attack should move.")]
    public SpecialMovementType movementType = SpecialMovementType.None;
    [Space(2)]
    [Header("Homing Settings")]
    [Tooltip("How long homing will track before it drops.")]
    public float moveTimer = 2f;
    
    [Tooltip("How quickly homing turns toward the player.")]
    public float homingTurnSpeed = 4f;

    [Tooltip("How strongly homing pulls upward when recovering height.")]
    public float homingRecoverUpStrength = 1.5f;
    #endregion

    #region Dive
    [Space(2)]
    [Header("Dive Settings")]
    [Tooltip("Maximum number of air loops before the dive attack starts. Minimum is always 1.")]
    public int maxLoops = 1;

    [Tooltip("Extra speed added when the projectile drops straight down in dive attack mode.")]
    public float attackSpeedBoost = 3f;

    [Tooltip("Radius size for the dive circle loop.")]
    public float circleRadius = 2f;
    #endregion

    #region Explosion
    [Space(2)]
    [Header("Explosion Settings")]
    [Tooltip("If true, this prefab will explode after a delay.")]
    public bool explodes = false;

    [Tooltip("Explosion radius (only used if explodes = true).")]
    public float explosionRadius = 1f;

    [Tooltip("Time before the explosion triggers (only used if explodes = true).")]
    public float explosionTimer = 2f;

    [Header("Growth Settings")]
    [Tooltip("If true, this prefab will grow over time until max size is reached.")]
    public bool enableGrowth = false;

    [Tooltip("Growth speed per second (only used if enableGrowth = true).")]
    public float growSpeed = 1f;

    [Tooltip("Maximum size scale (only used if enableGrowth = true).")]
    public float maxSize = 2f;
    #endregion

    #endregion
}
