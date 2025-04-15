using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraPanZone : MonoBehaviour
{
    public PanDirection panDirection = PanDirection.Right;
    public float panDistance = 3f;
    public float panTime = 0.5f;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (hasTriggered) return;
        hasTriggered = true;

        if (CameraZoneManager.instance != null)
        {
            //CameraZoneManager.instance.ApplyPan(panDirection, panDistance, panTime);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        hasTriggered = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw zone area in yellow
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // semi-transparent yellow
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box)
        {
            Vector3 center = transform.position + (Vector3)box.offset;
            Vector3 size = (Vector3)box.size;
            Gizmos.DrawCube(center, size);
        }

        // Draw directional pan line in cyan
        Gizmos.color = Color.cyan;
        Vector3 direction = Vector3.zero;

        switch (panDirection)
        {
            case PanDirection.Up: direction = Vector3.up; break;
            case PanDirection.Down: direction = Vector3.down; break;
            case PanDirection.Left: direction = Vector3.left; break;
            case PanDirection.Right: direction = Vector3.right; break;
        }

        Vector3 start = transform.position;
        Vector3 end = start + (direction * panDistance);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.15f);
    }
#endif
}
