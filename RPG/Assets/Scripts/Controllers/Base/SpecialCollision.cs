using System.Collections.Generic;
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

    private readonly List<Collider2D> hitCollisions = new();

    public void Setup(Rigidbody2D rigidbody)
    {
        rb = rigidbody;
        currentTargetLayerName = defaultTargetLayerName;

        hit = false;
        hitCollision = null;
        hitType = SpecialHitType.None;
    }

    #region Check Collider
    public void CheckCollisionShape(SpecialCollisionShape collisionShape)
    {
        switch (collisionShape)
        {
            case SpecialCollisionShape.Circle:
                CheckCircleCollider();
                break;

            case SpecialCollisionShape.Box:
                CheckBoxCollider();
                break;

            case SpecialCollisionShape.Capsule:
                CheckCapsuleCollider();
                break;
        }
    }

    private void CheckCircleCollider()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            DisableOtherColliders(circle);
            return;
        }

        Collider2D current = GetCurrentCollider();
        circle = gameObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;

        CopyToCircle(current, circle);
        DisableOtherColliders(circle);
    }

    private void CheckBoxCollider()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            DisableOtherColliders(box);
            return;
        }

        Collider2D current = GetCurrentCollider();
        box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        CopyToBox(current, box);
        DisableOtherColliders(box);
    }

    private void CheckCapsuleCollider()
    {
        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            DisableOtherColliders(capsule);
            return;
        }

        Collider2D current = GetCurrentCollider();
        capsule = gameObject.AddComponent<CapsuleCollider2D>();
        capsule.isTrigger = true;

        CopyToCapsule(current, capsule);
        DisableOtherColliders(capsule);
    }

    #region Copy
    private void CopyToCircle(Collider2D source, CircleCollider2D target)
    {
        if (source == null)
            return;

        if (source is CircleCollider2D sourceCircle)
        {
            target.offset = sourceCircle.offset;
            target.radius = sourceCircle.radius;
        }
        else if (source is BoxCollider2D sourceBox)
        {
            target.offset = sourceBox.offset;
            target.radius = Mathf.Min(sourceBox.size.x, sourceBox.size.y) * 0.5f;
        }
        else if (source is CapsuleCollider2D sourceCapsule)
        {
            target.offset = sourceCapsule.offset;
            target.radius = Mathf.Min(sourceCapsule.size.x, sourceCapsule.size.y) * 0.5f;
        }
    }

    private void CopyToBox(Collider2D source, BoxCollider2D target)
    {
        if (source == null)
            return;

        if (source is CircleCollider2D sourceCircle)
        {
            target.offset = sourceCircle.offset;
            float diameter = sourceCircle.radius * 2f;
            target.size = new Vector2(diameter, diameter);
        }
        else if (source is BoxCollider2D sourceBox)
        {
            target.offset = sourceBox.offset;
            target.size = sourceBox.size;
        }
        else if (source is CapsuleCollider2D sourceCapsule)
        {
            target.offset = sourceCapsule.offset;
            target.size = sourceCapsule.size;
        }
    }

    private void CopyToCapsule(Collider2D source, CapsuleCollider2D target)
    {
        if (source == null)
            return;

        if (source is CircleCollider2D sourceCircle)
        {
            target.offset = sourceCircle.offset;
            float diameter = sourceCircle.radius * 2f;
            target.size = new Vector2(diameter, diameter);
            target.direction = CapsuleDirection2D.Vertical;
        }
        else if (source is BoxCollider2D sourceBox)
        {
            target.offset = sourceBox.offset;
            target.size = sourceBox.size;
            target.direction = sourceBox.size.x >= sourceBox.size.y
                ? CapsuleDirection2D.Horizontal
                : CapsuleDirection2D.Vertical;
        }
        else if (source is CapsuleCollider2D sourceCapsule)
        {
            target.offset = sourceCapsule.offset;
            target.size = sourceCapsule.size;
            target.direction = sourceCapsule.direction;
        }
    }
    #endregion

    #region Helper Methods
    private Collider2D GetCurrentCollider()
    {
        Collider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
            return collider;

        collider = GetComponent<BoxCollider2D>();
        if (collider != null)
            return collider;

        collider = GetComponent<CapsuleCollider2D>();
        return collider;
    }

    private void DisableOtherColliders(Collider2D activeCollider)
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.enabled = circle == activeCollider;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            box.enabled = box == activeCollider;

        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
            capsule.enabled = capsule == activeCollider;
    }
    #endregion

    #endregion

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

    #region Overlap Methods
    public Collider2D[] GetOverlapHits(SpecialCollisionShape explosionShape)
    {
        hitCollisions.Clear();

        switch (explosionShape)
        {
            case SpecialCollisionShape.Circle:
                GetCircleOverlapHits();
                break;

            case SpecialCollisionShape.Box:
                GetBoxOverlapHits();
                break;

            case SpecialCollisionShape.Capsule:
                GetCapsuleOverlapHits();
                break;
        }

        return hitCollisions.ToArray();
    }

    private void GetCircleOverlapHits()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        Vector2 center = circle.bounds.center;
        float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        AddOverlapHits(Physics2D.OverlapCircleAll(center, radius));
    }

    private void GetBoxOverlapHits()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Vector2 center = box.bounds.center;
        Vector2 size = box.bounds.size;
        float angle = transform.eulerAngles.z;

        AddOverlapHits(Physics2D.OverlapBoxAll(center, size, angle));
    }

    private void GetCapsuleOverlapHits()
    {
        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule == null)
            return;

        Vector2 center = capsule.bounds.center;
        Vector2 size = capsule.bounds.size;
        float angle = transform.eulerAngles.z;

        AddOverlapHits(Physics2D.OverlapCapsuleAll(center, size, capsule.direction, angle));
    }

   
    private void AddOverlapHits(Collider2D[] overlaps)
    {
        if (overlaps == null || overlaps.Length == 0)
            return;

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null || overlap.transform == transform)
                continue;

            if (!hitCollisions.Contains(overlap))
                hitCollisions.Add(overlap);
        }
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