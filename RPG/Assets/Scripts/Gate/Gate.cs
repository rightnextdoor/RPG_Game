using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Gate : MonoBehaviour
{
    [SerializeField] private GateData gateData;
    [SerializeField] private UnityEvent unlockGate;

    private void Start()
    {
        if (!gateData.isLocked)
            unlockGate?.Invoke();
    }

    public void UnlockGate()
    {
        gateData.isLocked = false;
        GateManager.instance.GateUnlocked(gateData);
        unlockGate?.Invoke();
    }
}
