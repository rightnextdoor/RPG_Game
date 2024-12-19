using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class NPC : NPCEntity
{
    [Header("Move info")]
    public float moveSpeed = 1.5f;
    public float idleTime = 2;
    public Transform[] waypoints;
    [HideInInspector] public int wavePointIndex = 0;
    [HideInInspector] public Transform target;

    public NPCStateMachine stateMachine { get; private set; }
    public string lastAnimBoolName { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new NPCStateMachine();
    }

    protected override void Start()
    {
        base.Start();

        target = waypoints[wavePointIndex];
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
    }
    
    public virtual void AssignLastAnimName(string _animBoolName) => lastAnimBoolName = _animBoolName;
    public virtual void AnimationFinishTrigger() => stateMachine.currentState.AnimationFinishTrigger();
}
