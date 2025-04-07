using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundLibrary", menuName = "Audio/Sound FX Library")]
public class SoundLibrary : ScriptableObject
{
    public string soundName;
    public List<Sound> sounds = new List<Sound>();
}
