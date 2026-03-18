using UnityEngine;

public class SpecialCollision : MonoBehaviour
{
    #region Setup

    [Header("Layer Setup")]
    [SerializeField] private string defaultTargetLayerName = "Player";
    [SerializeField] private string groundLayerName = "Ground";
    [SerializeField] private string platformTag = "Platform";
    private Rigidbody2D rb;

    private string currentTargetLayerName;

    private bool hit;
    private Collider2D hitCollision;
    private SpecialHitType hitType;

    public void Setup(Rigidbody2D rigidbody)
    {
        rb = rigidbody;
        currentTargetLayerName = defaultTargetLayerName;

        hit = false;
        hitCollision = null;
        hitType = SpecialHitType.None;
    }

    #endregion

    #region Collider

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hit)
            return;

        if (IgnoreHit(collision))
            return;

        if (GoodHit(collision, out SpecialHitType type))
        {
            hit = true;
            hitCollision = collision;
            hitType = type;
        }
    }

    public bool TryGetHit(out Collider2D collision, out SpecialHitType type)
    {
        if (!hit)
        {
            collision = null;
            type = SpecialHitType.None;
            return false;
        }

        collision = hitCollision;
        type = hitType;

        return true;
    }

    public void ClearHit()
    {
        hit = false;
        hitCollision = null;
        hitType = SpecialHitType.None;
    }

    private bool GoodHit(Collider2D collision, out SpecialHitType type)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(currentTargetLayerName))
        {
            type = SpecialHitType.Target;
            return true;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer(groundLayerName))
        {
            type = SpecialHitType.Ground;
            return true;
        }

        type = SpecialHitType.None;
        return false;
    }

    private bool IgnoreHit(Collider2D collision)
    {
        if (collision.CompareTag(platformTag))
            return true;

        return false;
    }

    #endregion

    #region Other Methods
    public void ChangeTargetLayer(string newTargetLayerName)
    {
        currentTargetLayerName = newTargetLayerName;
    }

    public void StuckInto()
    {
        ParticleSystem particleSystem = GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
            particleSystem.Stop();

        Collider2D projectileCollider = GetComponent<Collider2D>();
        if (projectileCollider != null)
            projectileCollider.enabled = false;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        transform.parent = hitCollision.transform;
    }

    #endregion
}

public enum SpecialHitType
{
    None,
    Target,
    Ground
}