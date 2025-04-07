using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewZoneMusicLibrary", menuName = "Audio/Zone Music Library")]
public class ZoneMusicLibrary : ScriptableObject
{
    public string zoneName;
    public List<Sound> musicTracks = new List<Sound>();
}
