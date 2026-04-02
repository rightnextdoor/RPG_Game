using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StateSound
{
    public string name;
    public List<int> soundPoints;
    public float delay;
    public bool useTransform;
    public float? distance;

    public StateSound(string name, float delay = 0f, bool useTransform = false, float? distance = null)
    {
        this.name = name;
        this.soundPoints = new List<int>();
        this.delay = delay;
        this.useTransform = useTransform;
        this.distance = distance;
    }

    public void Play(Transform defaultTransform)
    {
        var src = useTransform ? defaultTransform : null;

        if (delay <= 0f)
        {
            if (distance.HasValue)
                AudioManager.instance.PlaySFXWithDelay(name, 0f, src, distance.Value);
            else
                AudioManager.instance.PlaySFX(name, src);
        }
        else
        {
            if (distance.HasValue)
                AudioManager.instance.PlaySFXWithDelay(name, delay, src, distance.Value);
            else
                AudioManager.instance.PlaySFXWithDelay(name, delay, src);
        }
    }
}