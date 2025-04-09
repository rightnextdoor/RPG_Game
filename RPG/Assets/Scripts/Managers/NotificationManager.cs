using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance { get; private set; }

    [Header("Main Notifications")]
    public GameObject notificationPrefab;
    public Transform notificationParent;

    [Header("Special Notifications")]
    public GameObject specialNotificationPrefab;
    public Transform specialNotificationParent;
    public float specialDisplayDuration = 2f;

    [Header("Notification Settings")]
    public float displayDuration = 3f;

    [Header("Color Pool")]
    public List<Color> defaultColors = new List<Color>()
    {
        new Color(0.8f, 0.2f, 0.2f, 0.5f), // Red
        new Color(0.2f, 0.2f, 0.8f, 0.5f), // Blue
        new Color(0.2f, 0.8f, 0.2f, 0.5f)  // Green
    };

    private const int maxStackSize = 3;
    private List<Color> usedColors = new List<Color>();
    private Dictionary<GameObject, Color> notificationColorMap = new Dictionary<GameObject, Color>();
    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private List<GameObject> activeNotifications = new List<GameObject>();

    private GameObject activeSpecialNotification;

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

    // Main stackable notifications
    public void ShowNotification(string message, Sprite icon = null)
    {
        if (activeNotifications.Count >= maxStackSize)
        {
            notificationQueue.Enqueue(new NotificationData(message, icon));
            return;
        }

        Color colorToUse = GetNextAvailableColor();
        NotificationData data = new NotificationData(message, icon, colorToUse);
        CreateNotification(data);
    }

    // Special single notification
    public void ShowSpecialNotification(string message, Sprite icon = null, Color? color = null)
    {
        specialNotificationQueue.Enqueue(new SpecialNotificationData(message, icon, color));
        if (!isDisplayingSpecialNotification)
            StartCoroutine(ProcessSpecialNotificationQueue());
    }

    private IEnumerator ProcessSpecialNotificationQueue()
    {
        isDisplayingSpecialNotification = true;

        while (specialNotificationQueue.Count > 0)
        {
            var data = specialNotificationQueue.Dequeue();

            GameObject notifObj = Instantiate(specialNotificationPrefab, specialNotificationParent);
            CanvasGroup canvasGroup = notifObj.GetComponent<CanvasGroup>();
            Image background = notifObj.GetComponent<Image>();
            Image icon = notifObj.transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI messageText = notifObj.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();

            //FIXED background stays the same, no need to assign color
            background.color = new Color(0f, 0f, 0f, 0.75f); // dark semi-transparent background

            //Set the message and its TEXT color
            messageText.text = data.message;
            messageText.color = data.textColor;

            if (data.icon != null)
            {
                icon.sprite = data.icon;
                icon.gameObject.SetActive(true);
            }
            else
            {
                icon.gameObject.SetActive(false);
            }


            // Fade In
            canvasGroup.alpha = 0;
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, t / 0.25f);
                yield return null;
            }

            yield return new WaitForSeconds(2f); // visible duration

            // Fade Out
            t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1, 0, t / 0.5f);
                yield return null;
            }

            Destroy(notifObj);
            yield return new WaitForEndOfFrame(); // brief delay between messages
        }

        isDisplayingSpecialNotification = false;
    }

    private Color GetNextAvailableColor()
    {
        foreach (Color c in defaultColors)
        {
            if (!usedColors.Contains(c))
            {
                usedColors.Add(c);
                return c;
            }
        }
        return defaultColors[defaultColors.Count - 1]; // Fallback
    }

    private void ReleaseColor(Color color)
    {
        usedColors.Remove(color);
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

        notificationColorMap[notifGO] = data.backgroundColor;
        activeNotifications.Add(notifGO);
        StartCoroutine(DisplayNotification(notifGO, displayDuration));
    }

    private IEnumerator DisplayNotification(GameObject notificationGO, float duration)
    {
        yield return new WaitForSeconds(duration);

        CanvasGroup group = notificationGO.GetComponent<CanvasGroup>();
        if (group == null) yield break;

        float fadeTime = 1f;
        float t = 0f;

        while (t < fadeTime)
        {
            if (group == null) yield break;

            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        activeNotifications.Remove(notificationGO);

        if (notificationColorMap.TryGetValue(notificationGO, out Color releasedColor))
        {
            ReleaseColor(releasedColor);
            notificationColorMap.Remove(notificationGO);
        }

        Destroy(notificationGO);

        // Reorder siblings
        for (int i = 0; i < activeNotifications.Count; i++)
        {
            activeNotifications[i].transform.SetSiblingIndex(i);
        }

        // Add next in queue
        if (notificationQueue.Count > 0)
        {
            NotificationData next = notificationQueue.Dequeue();
            next.backgroundColor = GetNextAvailableColor();
            CreateNotification(next);
        }
    }

    private IEnumerator FadeOutAndDestroy(GameObject go, float duration)
    {
        yield return new WaitForSeconds(duration);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null) yield break;

        float t = 0f;
        float fadeTime = 1f;

        while (t < fadeTime)
        {
            if (group == null) yield break;

            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        if (go == activeSpecialNotification)
            activeSpecialNotification = null;

        Destroy(go);
    }

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

        public NotificationData(string message, Sprite icon)
        {
            this.message = message;
            this.icon = icon;
        }
    }

    private Queue<SpecialNotificationData> specialNotificationQueue = new Queue<SpecialNotificationData>();
    private bool isDisplayingSpecialNotification = false;

    [System.Serializable]
    public class SpecialNotificationData
    {
        public string message;
        public Sprite icon;
        public Color textColor;

        public SpecialNotificationData(string message, Sprite icon = null, Color? textColor = null)
        {
            this.message = message;
            this.icon = icon;
            this.textColor = textColor ?? Color.yellow; // default text color
        }
    }


}
