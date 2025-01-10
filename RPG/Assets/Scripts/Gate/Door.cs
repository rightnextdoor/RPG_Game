using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    [ContextMenu("Open")]
    public void Open()
    {
        anim.SetTrigger("Open");
    }
}
