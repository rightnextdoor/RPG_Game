using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Gate Data", menuName = "Data/Gate")]
public class GateData : ScriptableObject
{
    public string gateId;
    public bool isLocked = true;

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        gateId = AssetDatabase.AssetPathToGUID(path);
#endif
    }
}
