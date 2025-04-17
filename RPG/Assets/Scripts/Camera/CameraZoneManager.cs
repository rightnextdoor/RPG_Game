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
    public CinemachineVirtualCamera panCam;

    [Header("Shared Follow Target")]
    public Transform horizontalTarget;
    public Transform verticalTarget;
    public Transform fixedTarget;
    public Transform panTarget;
    public Transform cutsceneCamTarget;

    [Header("Zone Follow Smoothing")]
    public float zoneTargetSmoothSpeed = 10f;

    [Header("Falling YDamping Settings")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallYPanTime = 0.35f;
    [SerializeField] private float fallSpeedYDampingChangeThreshold = -15f;

    [Header("Falling Offset Settings")]
    [SerializeField] private float fallOffsetY = -10f;
    [SerializeField] private float fallOffsetLerpTime = 0.35f;
    [SerializeField] private float moveOffsetX = 7f;

    private Vector2 defaultOffset;

    private Vector3 zoneTargetVelocity = Vector3.zero;
    private Transform player;
    private Rigidbody2D playerRB;
    private CameraZone currentZone;
    private bool initialized = false;

    private float normYPanAmount;
    private bool isLerpingYDamping;
    private Coroutine lerpYDampingCoroutine;
    private Coroutine offsetLerpCoroutine;

    private Coroutine panCoroutine;
    private bool isPanning;

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

        while (PlayerManager.instance == null || PlayerManager.instance.player == null || !PlayerManager.instance.player.gameObject.activeInHierarchy)
        {
            yield return null;
        }

        player = PlayerManager.instance.player.transform;
        playerRB = PlayerManager.instance.player.GetComponent<Rigidbody2D>();
        
        EnableCameras();
        SetupPlayerCam();
        SetupHorizontalCam();
        SetupVerticalCam();
        SetupFixedCam();
        SetupPanCam();

        initialized = true;

        yield return new WaitForSeconds(0.05f);
        RestorePlayerCamDamping();
    }

    private void EnableCameras()
    {
        if (playerCam != null) playerCam.enabled = true;
        if (fixedCam != null) fixedCam.enabled = true;
        if (horizontalCam != null) horizontalCam.enabled = true;
        if (verticalCam != null) verticalCam.enabled = true;
        if (panCam != null) panCam.enabled = true;
    }


    // Call this at scene start if using a cutscene to force camera to snap to the spawn position
    public void PrepareForCutscene(Vector3 spawnPosition)
    {
        EnableCameras();
        if (cutsceneCamTarget == null || playerCam == null) return;

        cutsceneCamTarget.position = new Vector3(spawnPosition.x, spawnPosition.y, -10f);
        playerCam.Follow = cutsceneCamTarget;

        // Ensure Transposer is initialized
        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
        {
            // Set default transposer values just like SetupPlayerCam
            transposer.m_XDamping = 2f;
            transposer.m_YDamping = 2f;
            transposer.m_DeadZoneWidth = 0.1f;
            transposer.m_DeadZoneHeight = 0.1f;
            transposer.m_SoftZoneWidth = 0.8f;
            transposer.m_SoftZoneHeight = 0.8f;
            transposer.m_ScreenY = 0.65f;
        }

        playerCam.PreviousStateIsValid = false;
        playerCam.OnTargetObjectWarped(cutsceneCamTarget, Vector3.zero);
        playerCam.Priority = 20;
    }

    public void ForceSnap()
    {
        if (playerCam != null && playerCam.Follow != null)
        {
            playerCam.OnTargetObjectWarped(playerCam.Follow, Vector3.zero);
            playerCam.PreviousStateIsValid = false;
        }
    }

    private void SetupPlayerCam()
    {
        if (playerCam == null || player == null) return;

        playerCam.Follow = player;

        var transposer = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = 2f;
        transposer.m_YDamping = 2f;
        transposer.m_DeadZoneWidth = 0.4f;
        transposer.m_DeadZoneHeight = 0.45f;
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
        if (horizontalCam == null || horizontalTarget == null) return;

        horizontalCam.Follow = horizontalTarget;
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
        if (verticalCam == null || verticalTarget == null) return;

        verticalCam.Follow = verticalTarget;
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
        if (fixedCam == null || fixedTarget == null) return;

        fixedCam.Follow = fixedTarget;
        fixedCam.Priority = 5;
    }
    private void SetupPanCam()
    {
        if (panCam == null || panTarget == null) return;

        panCam.Follow = panTarget;
        panCam.Priority = 5;

        var transposer = panCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = 2f;
        transposer.m_YDamping = 2f;
        transposer.m_DeadZoneWidth = 0f;
        transposer.m_DeadZoneHeight = 0f;
        transposer.m_SoftZoneWidth = 0.8f;
        transposer.m_SoftZoneHeight = 0.8f;
        transposer.m_ScreenX = 0.5f;
        transposer.m_ScreenY = 0.5f;
    }

    public void StartPan(PanDirection direction, float distance, float duration, Vector2 zoneCenter)
    {
        if (isPanning) return;
        if (panCoroutine != null) StopCoroutine(panCoroutine);
        panCoroutine = StartCoroutine(HandlePan(direction, distance, duration, zoneCenter));
    }

    private IEnumerator HandlePan(PanDirection direction, float distance, float duration, Vector2 zoneCenter)
    {
        isPanning = true;

        panTarget.position = new Vector3(zoneCenter.x, zoneCenter.y, -10f);

        SetPriority(panCam, 30);
        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        Vector3 start = panTarget.position;
        Vector3 offset = direction switch
        {
            PanDirection.Up => Vector3.up,
            PanDirection.Down => Vector3.down,
            PanDirection.Left => Vector3.left,
            PanDirection.Right => Vector3.right,
            _ => Vector3.zero
        } * distance;

        Vector3 end = start + offset;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            panTarget.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }
    }

    public void StopPan()
    {
        if (panCoroutine != null) StopCoroutine(panCoroutine);
        isPanning = false;
        SetPriority(panCam, 5);

        if (currentZone != null)
            ApplyZone(currentZone);
        else
            ResetToFreeFollow();
    }

    public void ApplyZone(CameraZone zone)
    {
        if (!initialized || player == null || zone == null) return;

        currentZone = zone;

        switch (zone.mode)
        {
            case CameraZone.ZoneMode.Fixed:
                if (fixedTarget != null)
                {
                    Vector2 center = zone.GetFixedCenter();
                    fixedTarget.position = new Vector3(center.x, center.y, -10f);
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
                if (horizontalTarget != null)
                {
                    float centerY = zone.GetCenterY();
                    horizontalTarget.position = new Vector3(player.position.x, centerY, -10f);
                }

                SetPriority(horizontalCam, 20);
                SetPriority(playerCam, 10);
                SetPriority(fixedCam, 5);
                SetPriority(verticalCam, 5);
                break;

            case CameraZone.ZoneMode.VerticalOnly:
                if (verticalTarget != null)
                {
                    float centerX = zone.GetFixedCenter().x;
                    verticalTarget.position = new Vector3(centerX, player.position.y, -10f);
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
        if (player == null) return;

        // Move zone follow targets for horizontal/vertical/fixed zones
        if (horizontalCam != null && horizontalCam.Priority == 20 && currentZone != null && horizontalTarget != null)
        {
            float centerY = currentZone.GetCenterY();
            Vector3 targetPos = new Vector3(player.position.x, centerY, -10f);
            horizontalTarget.position = Vector3.SmoothDamp(horizontalTarget.position, targetPos, ref zoneTargetVelocity, 1f / zoneTargetSmoothSpeed);
        }
        else if (verticalCam != null && verticalCam.Priority == 20 && currentZone != null && verticalTarget != null)
        {
            float centerX = currentZone.GetFixedCenter().x;
            Vector3 targetPos = new Vector3(centerX, player.position.y, -10f);
            verticalTarget.position = Vector3.SmoothDamp(verticalTarget.position, targetPos, ref zoneTargetVelocity, 1f / zoneTargetSmoothSpeed);
        }
        else if (fixedCam != null && fixedCam.Priority == 20 && currentZone != null && fixedTarget != null)
        {
            Vector2 center = currentZone.GetFixedCenter();
            fixedTarget.position = new Vector3(center.x, center.y, -10f);
        }

        // Apply dynamic camera offset when in FreeFollow
        Player getPlayer = PlayerManager.instance.player;      
        if (playerCam != null && playerRB != null && playerCam.Priority == 20 && getPlayer != null)
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
            
            // Apply directional X offset when moving
            float offsetX = Mathf.Abs(playerRB.velocity.x) > 0.1f ? Mathf.Sign(playerRB.velocity.x) * (moveOffsetX * getPlayer.facingDir) : 0f;
            float offsetY = isFalling ? fallOffsetY : defaultOffset.y;

            Vector2 targetOffset = new Vector2(offsetX, offsetY);
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

public enum PanDirection
{
    Up,
    Down,
    Left,
    Right
}