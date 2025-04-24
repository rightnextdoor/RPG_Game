using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Data")]
    public CheckpointData checkpointData;

    [Header("Popup UI")]
    public GameObject popupUI;
    public float popupDisplayTime = 2f;

    [Header("Activation Animation")]
    private Animator animator;
    public string activateTrigger = "active";

    private bool playerInRange;
    private bool hasPlayedActivateAnimation = false;
    private bool menuOpen;

    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        UpdatePositionInSO();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            animator = GetComponent<Animator>();
            popupUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdatePositionInSO();
        }
#endif

        if (Application.isPlaying)
        {
            menuOpen = UIManager.instance.GetUICheckpoint().IsMenuOpen();
            if (playerInRange && Input.GetKeyDown(KeyCode.W) && !menuOpen)
            {
                if (!checkpointData.isActivated)
                {
                    AudioManager.instance.PlaySFX("Checkpoint", transform);                  
                    PlayActivateAnimation();
                }
                CheckpointManager.instance?.SaveCheckpoint(checkpointData);
                UIManager.instance.GetUICheckpoint().OpenMenu();
            }          
        }

        if (Application.isPlaying && checkpointData.isActivated && !hasPlayedActivateAnimation)
        {
            PlayActivateAnimation();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying || checkpointData == null) return;
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (popupUI != null)
        {
            popupUI.SetActive(true);
            StartCoroutine(HidePopupAfterDelay());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!Application.isPlaying || checkpointData == null) return;
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (popupUI != null)
        {
            popupUI.SetActive(false);
        }
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDisplayTime);
        if (popupUI != null && !playerInRange)
        {
            popupUI.SetActive(false);
        }
    }

    private void PlayActivateAnimation()
    {
        if (animator != null && !hasPlayedActivateAnimation)
        {
            animator.SetBool(activateTrigger, true);
            hasPlayedActivateAnimation = true;
        }
    }

    private void UpdatePositionInSO()
    {
        if (checkpointData == null) return;

        if (checkpointData.checkpointPosition != transform.position)
        {
            checkpointData.checkpointPosition = transform.position;

#if UNITY_EDITOR
            EditorUtility.SetDirty(checkpointData);
#endif
        }
    }
}
