using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion_AnimationTriggers : MonoBehaviour
{
    private Explosion explosion => GetComponentInParent<Explosion>();

    private void AnimationExplodeEvent() => explosion.AnimationExplodeEvent();
    private void SelfDestroy() => explosion.SelfDestroy();
}
