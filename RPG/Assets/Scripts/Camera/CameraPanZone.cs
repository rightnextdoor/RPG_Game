using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraPanZone : MonoBehaviour
{
    public PanDirection direction = PanDirection.Right;
    public float panDistance = 3f;
    public float panTime = 0.5f;
    public float delayBeforePan = 0.2f;

    private Coroutine delayCoroutine;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (delayCoroutine != null)
            StopCoroutine(delayCoroutine);

        delayCoroutine = StartCoroutine(DelayedPan());
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (delayCoroutine != null)
            StopCoroutine(delayCoroutine);

        if (CameraZoneManager.instance != null)
            CameraZoneManager.instance.StopPan();
    }

    private IEnumerator DelayedPan()
    {
        yield return new WaitForSeconds(delayBeforePan);

        Vector2 center = GetComponent<Collider2D>().bounds.center;

        if (CameraZoneManager.instance != null)
            CameraZoneManager.instance.StartPan(direction, panDistance, panTime, center);
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
