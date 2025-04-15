using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraZone : MonoBehaviour
{
    public enum ZoneMode { FreeFollow, HorizontalOnly, VerticalOnly, Fixed }
    public ZoneMode mode = ZoneMode.FreeFollow;

    [Tooltip("Optional transition duration (not currently used).")]
    public float transitionDuration = 0.6f;

    private BoxCollider2D col;

    private void Reset()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        if (col == null)
            col = GetComponent<BoxCollider2D>();
    }

    public Vector2 GetFixedCenter()
    {
        if (col == null)
            col = GetComponent<BoxCollider2D>();

        return col.bounds.center;
    }

    public float GetCenterY()
    {
        if (col == null)
            col = GetComponent<BoxCollider2D>();

        return col.bounds.center.y;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CameraZoneManager.instance?.ApplyZone(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CameraZoneManager.instance?.ResetToFreeFollow();
    }

    private void OnDrawGizmos()
    {
        if (col == null)
            col = GetComponent<BoxCollider2D>();

        if (!col) return;

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
        Gizmos.color = Color.white;
    }
}
