using UnityEngine;

[System.Serializable]
public class AttackSpawnSpec
{
    [Header("Prefab Basics")]
    [Tooltip("The name of the prefab to spawn.")]
    public string name;
    [Tooltip("The projectile or special attack prefab to spawn.")]
    public GameObject prefab;

    [Header("Control")]
    [Tooltip("The special attack controller used to run this attack.")]
    public SpecialAttackControl control;

    [Tooltip("Initial speed for the projectile (usually multiplied by facingDir).")]
    public float speed = 5f;

    [Tooltip("If true, the prefab will only spawn once per ON event (OFF resets it).")]
    public bool spawnOncePerOn = true;

    [Tooltip("Optional SFX cue name to play when this prefab is spawned.")]
    public string sfxOnSpawn;

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
}
