using System;
using UnityEngine;

public enum EnemyStateType
{
    Attack,
    Battle,
    Dead,
    Evasion,
    Idle,
    Jump,
    Move,
    Stunned,
    Teleport,
    RunIntoPlayer,
    BounceIdle,
    BounceMove,
    BounceBattle
}

[Serializable]
public class StateDetail
{
    #region Shared
    [Header("Shared")]
    public string name;
    public string animBoolName;
    public EnemyStateType stateType;
    #endregion

    #region Sounds
    [Space(2)]
    [Header("Sounds")]
    public StateSound[] enterSounds;
    public StateSound[] exitSounds;
    #endregion
}