using System.Collections.Generic;
using UnityEngine;

public partial class BounceStateBase<TEnemy> : TypedEnemyState<TEnemy> where TEnemy : Enemy_Regular
{
    #region Bounce phases

    protected virtual void AttachPhase()
    {
        SurfacePhase();

        bool isAligned = movementHandler.IsFeetAlignedToSurface();
        if (!isAligned)
            return;

        hasLandingHit = false;
        movementHandler.ClearIgnoredLaunchSurfaces();
        currentPhase = BouncePhase.Complete;
        OnAttachComplete();
    }

    protected virtual void SurfacePhase()
    {
        SetAttachedGravity();
        enemy.SetZeroVelocity();
        movementHandler.RotateFeetToSurface();
    }

    protected virtual void LaunchPhase()
    {
        SetAirborneGravity();

        movementHandler.CacheIgnoredSecondarySurface();

        Vector2 launchDirection = currentSurfaceNormal.normalized;
        Vector2 targetPosition = rb.position + launchDirection * (movementHandler.MoveSpeed() * Time.deltaTime);

        movementHandler.ApplyBounceMovement(targetPosition, launchDirection);

        if (!movementHandler.HasLeftCurrentSurface())
            return;

        currentPhase = BouncePhase.Airborne;
    }

    protected virtual void AirbornePhase()
    {
        movementHandler.MoveAirborne();

        movementHandler.UpdateIgnoredSecondarySurface();

        if (movementHandler.TryFindLandingSurface(out Vector2 hitPoint, out Vector2 hitNormal))
        {
            landingPoint = hitPoint;
            hasLandingHit = true;

            currentSurface = movementHandler.ClassifySurface(hitNormal);
            movementHandler.AlignToSurfaceNormal(hitNormal);

            currentPhase = BouncePhase.Attach;
            return;
        }
    }

    protected virtual void OnAttachComplete()
    {
    }
    protected virtual void CompletePhase()
    {
    }

    #endregion
    #region Gravity
    private void CacheDefaultGravity()
    {
        if (gravityCached)
            return;

        savedDefaultGravity = rb.gravityScale;
        gravityCached = true;
    }

    private void SetAttachedGravity()
    {
        rb.gravityScale = 0f;
    }

    private void SetAirborneGravity()
    {
        rb.gravityScale = savedDefaultGravity;
    }

    #endregion
}
