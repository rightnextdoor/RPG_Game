using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraPanZone : MonoBehaviour
{
    public PanDirection direction = PanDirection.Right;
    public float panDistance = 3f;
    public float panTime = 0.5f;
    public float delayBeforePan = 1f;

    private Coroutine delayCoroutine;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Snap panTarget immediately to zone center
        Vector2 center = GetComponent<Collider2D>().bounds.center;
        CameraZoneManager.instance.panTarget.position = new Vector3(center.x, center.y, -10f);

        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }
        delayCoroutine = StartCoroutine(DelayedPan(center));
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }
        CameraZoneManager.instance?.StopPan();
    }

    private IEnumerator DelayedPan(Vector2 center)
    {
        yield return new WaitForSeconds(delayBeforePan);
        CameraZoneManager.instance?.StartPan(direction, panDistance, panTime, center);
        delayCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // Yellow translucent
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            Gizmos.DrawCube(box.bounds.center, box.bounds.size);

        // Draw direction line
        Gizmos.color = Color.yellow;
        Vector2 dir = Vector2.zero;
        switch (direction)
        {
            case PanDirection.Up: dir = Vector2.up; break;
            case PanDirection.Down: dir = Vector2.down; break;
            case PanDirection.Left: dir = Vector2.left; break;
            case PanDirection.Right: dir = Vector2.right; break;
        }

        Gizmos.DrawLine(box.bounds.center, box.bounds.center + (Vector3)(dir * panDistance));
    }
}

