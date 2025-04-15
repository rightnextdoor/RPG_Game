using UnityEngine;

public class CameraYConstraint : MonoBehaviour
{
    public CameraZoneManager zoneManager;

    private void LateUpdate()
    {
        if (zoneManager == null || zoneManager.CurrentZone == null) return;

        if (zoneManager.CurrentZone.mode == CameraZone.ZoneMode.HorizontalOnly)
        {
            float centerY = zoneManager.CurrentZone.GetCenterY();
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, centerY, pos.z);
        }
    }
}
