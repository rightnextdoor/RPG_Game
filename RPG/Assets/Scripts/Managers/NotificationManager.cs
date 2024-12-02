using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance;

    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float fadeTime;
    [SerializeField] private GameObject notificationPopUp;

    private IEnumerator notificationCoroutine;
    private Queue<string> popupQueue;
    private Coroutine queueChecker;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        notificationPopUp.gameObject.SetActive(false);
        popupQueue = new Queue<string>();
    }

    public void SetNewNotification(string _message)
    {
        AddToQueue(_message);
    }

    private void AddToQueue(string text)
    {
        popupQueue.Enqueue(text);
        if (queueChecker == null)
            queueChecker = StartCoroutine(CheckQueue());
    }

    private void ShowPopup(string text)
    {
        notificationPopUp.gameObject.SetActive(true);
        notificationText.text = text;
        notificationCoroutine = FadeOutNotification(text);
        StartCoroutine(notificationCoroutine);
    }

    private IEnumerator CheckQueue()
    {
        do
        {
            ShowPopup(popupQueue.Dequeue());
            float t = 0;
            do
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            } while (t < fadeTime);
        }while (popupQueue.Count > 0);

        notificationPopUp.gameObject.SetActive(false);
        queueChecker = null;
    }

    private IEnumerator FadeOutNotification(string message)
    {
        notificationText.text = message;

        float t = 0;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            notificationText.color = new Color(
                notificationText.color.r,
                notificationText.color.g,
                notificationText.color.b,
                Mathf.Lerp(1f, 0f, t / fadeTime));
            yield return null;
        }
    }
}
