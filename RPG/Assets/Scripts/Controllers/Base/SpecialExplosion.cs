using UnityEngine;

public class SpecialExplosion : MonoBehaviour
{
    #region Setup

    private AttackSpawnSpec spec;

    private bool canGrow;
    private bool useExplosionTimer;

    private bool explosionStarted;
    private bool growActive;
    private bool explosionFinished;
    private bool explosionTimerActive;

    private bool explosionStartConsumed;
    private bool explosionFinishConsumed;

    private float lifeTimer;
    private float explosionTimer;

    private float growSpeed;
    private float maxSize;

    public void Setup(AttackSpawnSpec _spec)
    {
        spec = _spec;

        if (spec == null)
            return;

        canGrow = spec.canGrow;
        useExplosionTimer = spec.useExplosionTimer;

        lifeTimer = spec.lifeTimer;

        ConfigureExplosion();
    }

    #region Configure

    private void ConfigureExplosion()
    {
        ConfigureExplosionTimer();
        ConfigureGrow();
    }

    private void ConfigureExplosionTimer()
    {
        if (!useExplosionTimer || spec == null)
            return;

        explosionTimer = spec.explosionTimer;
    }

    private void ConfigureGrow()
    {
        if (!canGrow || spec == null)
            return;

        growSpeed = spec.growSpeed;
        maxSize = spec.maxSize;
    }

    #endregion

    #endregion

    #region Start Explosion
    private void Update()
    {
        UpdateLifeTimer();

        if (growActive)
            UpdateGrowth();

        if (explosionTimerActive)
            UpdateExplosionTimer();
    }

    private void UpdateLifeTimer()
    {
        if (explosionStarted)
            return;

        lifeTimer -= Time.deltaTime;

        if (lifeTimer < 0f)
            StartExplosion();
    }

    public void StartExplosion()
    {
        if (explosionStarted)
            return;

        explosionStarted = true;
        explosionStartConsumed = false;

        if (canGrow)
            growActive = true;

        if (useExplosionTimer)
            explosionTimerActive = true;
    }

    public bool ExplosionStarted()
    {
        if (!explosionStarted || explosionStartConsumed)
            return false;

        explosionStartConsumed = true;
        return true;
    }

    #endregion

    #region Finish Explosion

    public void FinishExplosion()
    {
        if (explosionFinished)
            return;

        growActive = false;
        explosionTimerActive = false;

        explosionFinished = true;
        explosionFinishConsumed = false;
    }

    public bool ExplosionFinished()
    {
        if (!explosionFinished || explosionFinishConsumed)
            return false;

        explosionFinishConsumed = true;
        return true;
    }

    #endregion

    #region Growth

    private void UpdateGrowth()
    {
        transform.localScale = Vector2.Lerp(
            transform.localScale,
            new Vector2(maxSize, maxSize),
            growSpeed * Time.deltaTime
        );

        if (maxSize - transform.localScale.x < 0.5f)
        {
            growActive = false;
            FinishExplosion();
        }
    }

    #endregion

    #region Explosion Timer

    private void UpdateExplosionTimer()
    {
        explosionTimer -= Time.deltaTime;

        if (explosionTimer < 0f)
        {
            explosionTimerActive = false;
            FinishExplosion();
        }
    }

    #endregion
}