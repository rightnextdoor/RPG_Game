using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraZoneManager : MonoBehaviour
{
    public static CameraZoneManager instance;

    [Header("Cinemachine Virtual Cameras")]
    public CinemachineVirtualCamera playerCam;
    public CinemachineVirtualCamera fixedCam;
    public CinemachineVirtualCamera horizontalCam;
    public CinemachineVirtualCamera verticalCam;

    [Header("Shared Follow Target")]
    public Transform zoneFollowTarget;

    [Header("Zone Follow Smoothing")]
    public float zoneTargetSmoothSpeed = 10f;

    [Header("Falling YDamping Settings")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallYPanTime = 0.35f;
    [SerializeField] private float fallSpeedYDampingChangeThreshold = -15f;

    [Header("Falling Offset Settings")]
    [SerializeField] private float fallOffsetY = -10f;
    [SerializeField] private float fallOffsetLerpTime = 0.35f;

    private Vector3 zoneTargetVelocity = Vector3.zero;
    private Transform player;
    private Rigidbody2D playerRB;
    private CameraZone currentZone;
    private bool initialized = false;

    private float normYPanAmount;
    private bool isLerpingYDamping;
    private Coroutine lerpYDampingCoroutine;

    private Vector2 defaultOffset;
    private Coroutine offsetLerpCoroutine;

    private Coroutine panCoroutine;

    public CameraZone CurrentZone => currentZone;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private IEnumerator Start()
    {
        yield return null;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[CameraZoneManager] Player not found.");
            yield break;
        }

        player = playerObj.transform;
        playerRB = player.GetComponent<Rigidbody2D>();

        SetupPlayerCam();
        SetupHorizontalCam();
        SetupVerticalCam();
        SetupFixedCam();

        initialized = true;

        yield return new WaitForSeconds(0.05f);
        RestorePlayerCamDamping();
    }

    private void SetupPlayerCam()
    {
        if (playerCam == null || player == null) return;

        playerCam.Follow = player;

        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = 2f;
        transposer.m_YDamping = 2f;
        transposer.m_DeadZoneWidth = 0.1f;
        transposer.m_DeadZoneHeight = 0.1f;
        transposer.m_SoftZoneWidth = 0.8f;
        transposer.m_SoftZoneHeight = 0.8f;
        transposer.m_ScreenY = 0.65f;

        normYPanAmount = transposer.m_YDamping;
        defaultOffset = transposer.m_TrackedObjectOffset;

        playerCam.PreviousStateIsValid = false;
        playerCam.OnTargetObjectWarped(player, Vector3.zero);
        playerCam.Priority = 20;
    }

    private void RestorePlayerCamDamping()
    {
        if (playerCam == null) return;

        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = 2f;
        transposer.m_YDamping = 2f;
    }

    private void SetupHorizontalCam()
    {
        if (horizontalCam == null || zoneFollowTarget == null) return;

        horizontalCam.Follow = zoneFollowTarget;
        horizontalCam.Priority = 10;

        var transposer = horizontalCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = 2f;
        transposer.m_YDamping = 0f;
        transposer.m_DeadZoneWidth = 0.05f;
        transposer.m_DeadZoneHeight = 10f;
        transposer.m_SoftZoneWidth = 0.6f;
        transposer.m_SoftZoneHeight = 0f;
        transposer.m_ScreenY = 0.5f;
    }

    private void SetupVerticalCam()
    {
        if (verticalCam == null || zoneFollowTarget == null) return;

        verticalCam.Follow = zoneFollowTarget;
        verticalCam.Priority = 5;

        var transposer = verticalCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = 0f;
        transposer.m_YDamping = 2f;
        transposer.m_DeadZoneWidth = 10f;
        transposer.m_DeadZoneHeight = 0.1f;
        transposer.m_SoftZoneWidth = 0f;
        transposer.m_SoftZoneHeight = 0.8f;
        transposer.m_ScreenX = 0.5f;
        transposer.m_ScreenY = 0.5f;
    }

    private void SetupFixedCam()
    {
        if (fixedCam == null || zoneFollowTarget == null) return;

        fixedCam.Follow = zoneFollowTarget;
        fixedCam.Priority = 5;
    }

    public void ApplyZone(CameraZone zone)
    {
        if (!initialized || player == null || zone == null) return;

        currentZone = zone;

        switch (zone.mode)
        {
            case CameraZone.ZoneMode.Fixed:
                if (zoneFollowTarget != null)
                {
                    Vector2 center = zone.GetFixedCenter();
                    zoneFollowTarget.position = new Vector3(center.x, center.y, -10f);
                }

                SetPriority(fixedCam, 20);
                SetPriority(playerCam, 10);
                SetPriority(horizontalCam, 5);
                SetPriority(verticalCam, 5);
                break;

            case CameraZone.ZoneMode.FreeFollow:
                currentZone = null;
                SetPlayerCam(2f, 2f);
                SetPriority(horizontalCam, 5);
                SetPriority(fixedCam, 5);
                SetPriority(verticalCam, 5);
                break;

            case CameraZone.ZoneMode.HorizontalOnly:
                if (zoneFollowTarget != null)
                {
                    float centerY = zone.GetCenterY();
                    zoneFollowTarget.position = new Vector3(player.position.x, centerY, -10f);
                }

                SetPriority(horizontalCam, 20);
                SetPriority(playerCam, 10);
                SetPriority(fixedCam, 5);
                SetPriority(verticalCam, 5);
                break;

            case CameraZone.ZoneMode.VerticalOnly:
                if (zoneFollowTarget != null)
                {
                    float centerX = zone.GetFixedCenter().x;
                    zoneFollowTarget.position = new Vector3(centerX, player.position.y, -10f);
                }

                SetPriority(verticalCam, 20);
                SetPriority(playerCam, 10);
                SetPriority(horizontalCam, 5);
                SetPriority(fixedCam, 5);
                break;
        }
    }

    public void ResetToFreeFollow()
    {
        currentZone = null;
        SetPlayerCam(2f, 2f);
        SetPriority(horizontalCam, 5);
        SetPriority(fixedCam, 5);
        SetPriority(verticalCam, 5);
    }

    private void SetPlayerCam(float xDamp, float yDamp)
    {
        if (playerCam == null) return;

        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = xDamp;
        transposer.m_YDamping = yDamp;
        transposer.m_DeadZoneWidth = 0.1f;
        transposer.m_DeadZoneHeight = 0.1f;
        transposer.m_SoftZoneWidth = 0.8f;
        transposer.m_SoftZoneHeight = 0.8f;
        transposer.m_ScreenY = 0.65f;

        playerCam.Priority = 20;
    }

    private void SetPriority(CinemachineVirtualCamera cam, int priority)
    {
        if (cam != null)
            cam.Priority = priority;
    }

    private void LateUpdate()
    {
        if (zoneFollowTarget == null || player == null) return;

        if (horizontalCam != null && horizontalCam.Priority == 20 && currentZone != null)
        {
            float centerY = currentZone.GetCenterY();
            Vector3 targetPos = new Vector3(player.position.x, centerY, -10f);
            zoneFollowTarget.position = Vector3.SmoothDamp(zoneFollowTarget.position, targetPos, ref zoneTargetVelocity, 1f / zoneTargetSmoothSpeed);
        }
        else if (verticalCam != null && verticalCam.Priority == 20 && currentZone != null)
        {
            float centerX = currentZone.GetFixedCenter().x;
            Vector3 targetPos = new Vector3(centerX, player.position.y, -10f);
            zoneFollowTarget.position = Vector3.SmoothDamp(zoneFollowTarget.position, targetPos, ref zoneTargetVelocity, 1f / zoneTargetSmoothSpeed);
        }
        else if (fixedCam != null && fixedCam.Priority == 20 && currentZone != null)
        {
            Vector2 center = currentZone.GetFixedCenter();
            zoneFollowTarget.position = new Vector3(center.x, center.y, -10f);
        }

        if (playerCam != null && playerRB != null)
        {
            var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            bool isFalling = playerRB.velocity.y < fallSpeedYDampingChangeThreshold;

            if (!isLerpingYDamping)
            {
                if (isFalling && transposer.m_YDamping != fallPanAmount)
                {
                    if (lerpYDampingCoroutine != null) StopCoroutine(lerpYDampingCoroutine);
                    lerpYDampingCoroutine = StartCoroutine(LerpYAction(true));
                }
                else if (!isFalling && transposer.m_YDamping != normYPanAmount)
                {
                    if (lerpYDampingCoroutine != null) StopCoroutine(lerpYDampingCoroutine);
                    lerpYDampingCoroutine = StartCoroutine(LerpYAction(false));
                }
            }

            Vector2 targetOffset = isFalling ? new Vector2(defaultOffset.x, fallOffsetY) : defaultOffset;
            if (offsetLerpCoroutine != null) StopCoroutine(offsetLerpCoroutine);
            offsetLerpCoroutine = StartCoroutine(LerpTrackedObjectOffset(targetOffset, fallOffsetLerpTime));
        }
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        isLerpingYDamping = true;

        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        float startDamp = transposer.m_YDamping;
        float endDamp = isPlayerFalling ? fallPanAmount : normYPanAmount;

        float t = 0f;
        while (t < fallYPanTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Lerp(startDamp, endDamp, t / fallYPanTime);
            transposer.m_YDamping = lerp;
            yield return null;
        }

        isLerpingYDamping = false;
    }

    private IEnumerator LerpTrackedObjectOffset(Vector2 targetOffset, float duration)
    {
        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        Vector2 startOffset = transposer.m_TrackedObjectOffset;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transposer.m_TrackedObjectOffset = Vector2.Lerp(startOffset, targetOffset, t / duration);
            yield return null;
        }
    }

}
