using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_BossTriggers : Enemy_AnimationTriggers
{
    private Enemy_Boss enemyBoss => GetComponentInParent<Enemy_Boss>();

    private void Relocate() => enemyBoss.FindPosition();
    private void MoveToPosition() => enemyBoss.MoveToPosition();
    private void MakeInvisible() => enemyBoss.fX.MakeTransprent(true,true,false);
    private void MakeVisible() => enemyBoss.fX.MakeTransprent(false, true, false);
}
