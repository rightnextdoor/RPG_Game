using UnityEngine;

public class NotificationTest : MonoBehaviour
{
    public Sprite testIcon;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            NotificationManager.instance.ShowNotification("This is a test message.");
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            NotificationManager.instance.ShowNotification("Message with icon!", testIcon);
        }
    }
}
