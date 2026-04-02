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

public enum SpecialCollisionShape
{
    Circle,
    Box,
    Capsule
}

public enum SpecialAnimationType
{
    None,
    Trigger,
    Bool
}

[System.Serializable]
public class AttackSpawnSpec
{
    [HideInInspector] public bool defaultsInitialized;
    #region Prefab

    [Header("Prefab Basics")]
    [Tooltip("The name of the prefab to spawn.")]
    public string name;

    public List<int> attackPoints = new();

    [Tooltip("The projectile or special attack prefab to spawn.")]
    public GameObject prefab;

    [Tooltip("Which overlap shape this explosion uses.")]
    public SpecialCollisionShape collisionShape = SpecialCollisionShape.Circle;

    public List<int> explodePoints = new();
    public StateSound[] sounds;

    #endregion

    #region Control

    [Space(2)]
    [Header("Control")]
    [Tooltip("The special attack controller used to run this attack.")]
    public SpecialAttackControl control;

    #region Movement

    [Space(6)]
    public bool canMove = false;
    [Space(2)]
    [Header("Movement")]
    public bool canParry = false;
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

    #endregion

    #region Explosion
    [Space(6)]
    [Tooltip("If true, this spawned attack uses explosion behavior.")]
    public bool explodes = false;

    [Space(2)]
    [Header("Explosion Settings")]
    [Tooltip("How long this projectile can live before it triggers the explosion if nothing hits it first.")]
    public float lifeTimer = 3f;

    [Tooltip("Fallback time before this object destroys itself after the explosion finishes, in case no animation destroy event is used.")]
    public float destroyAfterTime = 3f;

    [Space(2)]
    [Tooltip("If true, the explosion will grow before reaching its blast point.")]
    public bool canGrow = false;

    [Tooltip("How quickly the explosion grows toward its maximum size.")]
    public float growSpeed = 1f;

    [Tooltip("The maximum scale the explosion can grow to before moving to the next explosion step.")]
    public float maxSize = 1f;

    [Space(2)]
    [Tooltip("If true, the explosion uses a timer after it has already started.")]
    public bool useExplosionTimer = false;

    [Tooltip("How long the explosion waits after starting before it reaches its blast-ready point.")]
    public float explosionTimer = 1.5f;

    [Space(2)]
    [Tooltip("If true, the explosion uses animation as part of its explosion flow.")]
    public bool hasAnimation = false;

    [Tooltip("Which animator parameter type this explosion uses.")]
    public SpecialAnimationType animationType = SpecialAnimationType.None;

    [Tooltip("The animation bool name the child uses to start the explosion animation.")]
    public string animationBoolName;
    #endregion

    #endregion
}
