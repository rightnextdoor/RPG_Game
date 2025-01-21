using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
    private Animator anim;
    private Gate gate;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        gate = GetComponentInParent<Gate>();
    }

    public void UnlockGate()
    {
        gate.UnlockGate();
    }

 
    public void Unlock()
    {
        anim.SetTrigger("Open");
    }
}
