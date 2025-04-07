using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBackgroundMusicLibrary", menuName = "Audio/Background Music Library")]
public class BackgroundMusicLibrary : ScriptableObject
{
    public string backgroundName;
    public List<Sound> musicTracks = new List<Sound>();
}
