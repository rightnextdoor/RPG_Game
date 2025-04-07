using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZoneSceneMapping", menuName = "Audio/Zone Scene Mapping")]
public class ZoneSceneMapping : ScriptableObject
{
    [System.Serializable]
    public class ZoneDefinition
    {
        public string zoneName;
        public List<string> sceneNames;
    }

    public List<ZoneDefinition> zones = new List<ZoneDefinition>();
}
