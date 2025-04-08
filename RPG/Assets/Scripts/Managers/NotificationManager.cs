using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance { get; private set; }

    [Header("References")]
    public GameObject notificationPrefab; // Drag your prefab here
    public Transform notificationParent;  // Drag your NotificationPanel here

    [Header("Settings")]
    public float displayDuration = 3f;

    // Stack max size (should match backgroundColors.Length)
    private const int maxStackSize = 3;

    // Background colors for stack positions 0 (top), 1, 2 (bottom)
    private readonly Color[] backgroundColors = new Color[]
    {
        new Color(0.8f, 0.2f, 0.2f, 0.5f), // Red with alpha
        new Color(0.2f, 0.2f, 0.8f, 0.5f), // Blue
        new Color(0.2f, 0.8f, 0.2f, 0.5f)  // Green
    };

    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private List<GameObject> activeNotifications = new List<GameObject>();
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowNotification(string message, Sprite icon = null)
    {
        if (activeNotifications.Count >= maxStackSize)
        {
            notificationQueue.Enqueue(new NotificationData(message, icon, Color.clear));
            return;
        }

        Color assignedColor = backgroundColors[activeNotifications.Count];
        NotificationData data = new NotificationData(message, icon, assignedColor);

        CreateNotification(data);
    }

    private void CreateNotification(NotificationData data)
    {
        GameObject notifGO = Instantiate(notificationPrefab, notificationParent);
        notifGO.transform.SetSiblingIndex(activeNotifications.Count);

        Image background = notifGO.GetComponent<Image>();
        TMP_Text text = notifGO.transform.Find("MessageText").GetComponent<TMP_Text>();
        Image iconImage = notifGO.transform.Find("Icon").GetComponent<Image>();
        CanvasGroup canvasGroup = notifGO.GetComponent<CanvasGroup>();

        background.color = data.backgroundColor;
        text.text = data.message;
        canvasGroup.alpha = 1f;

        if (data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }

        activeNotifications.Add(notifGO);
        StartCoroutine(DisplayNotification(notifGO, displayDuration));
    }

    private IEnumerator DisplayNotification(GameObject notificationGO, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Fade out
        CanvasGroup group = notificationGO.GetComponent<CanvasGroup>();
        float fadeTime = 1f;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        activeNotifications.Remove(notificationGO);
        Destroy(notificationGO);

        // Reorder remaining notifications (but retain color)
        for (int i = 0; i < activeNotifications.Count; i++)
        {
            activeNotifications[i].transform.SetSiblingIndex(i);
        }

        // Handle queued messages
        if (notificationQueue.Count > 0)
        {
            Color nextColor = backgroundColors[activeNotifications.Count];
            NotificationData next = notificationQueue.Dequeue();
            next.backgroundColor = nextColor;
            CreateNotification(next);
        }
    }

    // Internal data holder
    private class NotificationData
    {
        public string message;
        public Sprite icon;
        public Color backgroundColor;

        public NotificationData(string message, Sprite icon, Color color)
        {
            this.message = message;
            this.icon = icon;
            this.backgroundColor = color;
        }
    }
}
