using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class CameraZone : MonoBehaviour
{
    public enum ZoneMode { FreeFollow, HorizontalOnly, VerticalOnly, Fixed }
    public ZoneMode mode = ZoneMode.FreeFollow;

    [Tooltip("Optional transition duration (not currently used).")]
    public float transitionDuration = 0.6f;

    [Header("Per-Zone Follow Target")]
    public Transform zoneTarget;

    private Collider2D col;

    // CameraZone.cs
    [Header("Framing Tweaks (optional, per zone)")]
    [Tooltip("Additive world offset on the locked Y (Fixed/Horizontal) or on the tracked Y (FreeFollow).")]
    public float yBias = 0f;

    [Tooltip("Additive world offset on the locked X (Fixed/Vertical) or on the tracked X (FreeFollow).")]
    public float xBias = 0f;

    // Optional FreeFollow-only overrides (handy tunnels/rooms where you want the player lower/higher on screen)
    [Header("FreeFollow Overrides (optional)")]
    public bool overrideTrackedOffset;     
    public Vector2 trackedOffset = Vector2.zero;
    public bool overrideScreenXY;         
    [Range(0f, 1f)] public float screenX = 0.5f;
    [Range(0f, 1f)] public float screenY = 0.5f;

    private void Reset()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (zoneTarget == null)
        {
            zoneTarget = transform;  // safe fallback; no new objects
            Debug.LogWarning($"[CameraZone] zoneTarget not assigned on '{name}'. Using the zone's transform.");
        }
        // Always keep the zone target centered on Awake
        if (zoneTarget != null && col != null)
        {
            var c = GetFixedCenter();
            zoneTarget.position = new Vector3(c.x, c.y, -10f);
        }
    }

    private void OnValidate()
    {
        // In editor, auto-center when values change
        if (!gameObject.activeInHierarchy) return;
        if (col == null) col = GetComponent<Collider2D>();
        if (zoneTarget != null && col != null)
        {
            var c = col.bounds.center;
            zoneTarget.position = new Vector3(c.x, c.y, -10f);
        }
    }



    public Transform GetZoneTarget()
    {
        if (col != null && zoneTarget != null)
        {
            var c = col.bounds.center;
            zoneTarget.position = new Vector3(c.x, c.y, -10f);
        }
        return zoneTarget;
    }

    public Vector2 GetFixedCenter()
    {
        return col.bounds.center;
    }

    public float GetCenterY()
    {
        return col.bounds.center.y;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CameraZoneManager.instance?.ApplyZone(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!Application.isPlaying)
            return;

        if (!other.CompareTag("Player"))
            return;

        var mgr = CameraZoneManager.instance;
        if (mgr == null || !mgr.isActiveAndEnabled)
            return;

        mgr.ExitZone(this);
    }

    private void OnDrawGizmos()
    {
        if (col == null)
            col = GetComponent<Collider2D>();
        if (!col) return;

        // Draw the zone area (same as before)
        Color color = mode switch
        {
            ZoneMode.FreeFollow => new Color(0f, 1f, 0f, 0.25f),
            ZoneMode.HorizontalOnly => new Color(0f, 1f, 1f, 0.25f),
            ZoneMode.VerticalOnly => new Color(1f, 0f, 1f, 0.25f),
            ZoneMode.Fixed => new Color(1f, 0f, 0f, 0.25f),
            _ => Color.gray
        };

        Gizmos.color = color;
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);

        // ---- Draw target marker as a black circle ----
        Transform t = zoneTarget != null ? zoneTarget : transform;
        Vector3 p = t.position;

        // radius scales with zone size but stays reasonable
        float r = Mathf.Max(0.08f, Mathf.Min(col.bounds.size.x, col.bounds.size.y) * 0.06f);

#if UNITY_EDITOR
        // Solid black disc with a thin white outline for visibility
        Handles.color = Color.black;
        Handles.DrawSolidDisc(p, Vector3.forward, r);
        Handles.color = Color.white;
        Handles.DrawWireDisc(p, Vector3.forward, r);
#else
    // Fallback in builds (Scene view uses editor path above)
    Gizmos.color = Color.black;
    Gizmos.DrawSphere(p, r);
#endif
    }

}
