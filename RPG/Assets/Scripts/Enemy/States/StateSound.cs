using UnityEngine;

public struct StateSound
{
    public string name;
    public float delay;
    public bool useTransform;
    public float distance;

    public StateSound(string name, float delay = 0f, bool useTransform = false, float distance = 15f)
    {
        this.name = name;
        this.delay = delay;
        this.useTransform = useTransform;
        this.distance = distance;
    }

    public void Play(Transform defaultTransform)
    {
        var src = useTransform ? defaultTransform : null;

        if (delay <= 0f)
        {
            if (distance != 15f)
                AudioManager.instance.PlaySFXWithDelay(name, 0f, src, distance);
            else
                AudioManager.instance.PlaySFX(name, src);
        }
        else
        {
            if (distance != 15f)
                AudioManager.instance.PlaySFXWithDelay(name, delay, src, distance);
            else
                AudioManager.instance.PlaySFXWithDelay(name, delay, src);
        }
    }
}
