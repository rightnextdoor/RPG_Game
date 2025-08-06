using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoneManager : MonoBehaviour
{
    public static CameraZoneManager instance;

    [Header("Cinemachine Virtual Cameras")]
    public CinemachineVirtualCamera playerCam;
    public CinemachineVirtualCamera fixedCam;
    public CinemachineVirtualCamera horizontalCam;
    public CinemachineVirtualCamera verticalCam;
    public CinemachineVirtualCamera panCam;

    [Header("Shared Follow Targets")]
    public Transform horizontalTarget;
    public Transform verticalTarget;
    public Transform fixedTarget;
    public Transform panTarget;
    public Transform cutsceneCamTarget;

    [Header("Player Camera Settings")]
    [Tooltip("Horizontal damping for the player camera framing transposer")]
    [SerializeField] private float playerCamXDamping = 2f;
    [Tooltip("Vertical damping for the player camera framing transposer")]
    [SerializeField] private float playerCamYDamping = 2f;
    [Tooltip("Dead zone width for the player camera")]
    [SerializeField] private float playerCamDeadZoneWidth = 0.2f;
    [Tooltip("Dead zone height for the player camera")]
    [SerializeField] private float playerCamDeadZoneHeight = 0.2f;
    [Tooltip("Soft zone width for the player camera")]
    [SerializeField] private float playerCamSoftZoneWidth = 0.6f;
    [Tooltip("Soft zone height for the player camera")]
    [SerializeField] private float playerCamSoftZoneHeight = 0.4f;
    [Tooltip("Screen X position (0–1) for the player camera")]
    [SerializeField] private float playerCamScreenX = 0.5f;
    [Tooltip("Screen Y position (0–1) for the player camera")]
    [SerializeField] private float playerCamScreenY = 0.65f;
    [Tooltip("Vertical offset applied below the player")]
    [SerializeField] private float playerCamOffsetY = -2f;

    [Header("Horizontal Camera Settings")]
    [Tooltip("Horizontal damping for the horizontal-only camera")]
    [SerializeField] private float horizontalCamXDamping = 2f;
    [Tooltip("Vertical damping for the horizontal-only camera")]
    [SerializeField] private float horizontalCamYDamping = 0f;
    [Tooltip("Dead zone width for the horizontal-only camera")]
    [SerializeField] private float horizontalCamDeadZoneWidth = 0.05f;
    [Tooltip("Dead zone height for the horizontal-only camera")]
    [SerializeField] private float horizontalCamDeadZoneHeight = 10f;
    [Tooltip("Soft zone width for the horizontal-only camera")]
    [SerializeField] private float horizontalCamSoftZoneWidth = 0.6f;
    [Tooltip("Soft zone height for the horizontal-only camera")]
    [SerializeField] private float horizontalCamSoftZoneHeight = 0f;
    [Tooltip("Screen X position (0–1) for the horizontal-only camera")]
    [SerializeField] private float horizontalCamScreenX = 0.5f;
    [Tooltip("Screen Y position (0–1) for the horizontal-only camera")]
    [SerializeField] private float horizontalCamScreenY = 0.5f;

    [Header("Vertical Camera Settings")]
    [Tooltip("Horizontal damping for the vertical-only camera")]
    [SerializeField] private float verticalCamXDamping = 0f;
    [Tooltip("Vertical damping for the vertical-only camera")]
    [SerializeField] private float verticalCamYDamping = 2f;
    [Tooltip("Dead zone width for the vertical-only camera")]
    [SerializeField] private float verticalCamDeadZoneWidth = 10f;
    [Tooltip("Dead zone height for the vertical-only camera")]
    [SerializeField] private float verticalCamDeadZoneHeight = 0.1f;
    [Tooltip("Soft zone width for the vertical-only camera")]
    [SerializeField] private float verticalCamSoftZoneWidth = 0f;
    [Tooltip("Soft zone height for the vertical-only camera")]
    [SerializeField] private float verticalCamSoftZoneHeight = 0.8f;
    [Tooltip("Screen X position (0–1) for the vertical-only camera")]
    [SerializeField] private float verticalCamScreenX = 0.5f;
    [Tooltip("Screen Y position (0–1) for the vertical-only camera")]
    [SerializeField] private float verticalCamScreenY = 0.5f;

    [Header("Pan Camera Settings")]
    [Tooltip("Horizontal damping for the pan camera framing transposer")]
    [SerializeField] private float panCamXDamping = 2f;
    [Tooltip("Vertical damping for the pan camera framing transposer")]
    [SerializeField] private float panCamYDamping = 2f;
    [Tooltip("Dead zone width for the pan camera")]
    [SerializeField] private float panCamDeadZoneWidth = 0f;
    [Tooltip("Dead zone height for the pan camera")]
    [SerializeField] private float panCamDeadZoneHeight = 0f;
    [Tooltip("Soft zone width for the pan camera")]
    [SerializeField] private float panCamSoftZoneWidth = 0.8f;
    [Tooltip("Soft zone height for the pan camera")]
    [SerializeField] private float panCamSoftZoneHeight = 0.8f;
    [Tooltip("Screen X position (0–1) for the pan camera")]
    [SerializeField] private float panCamScreenX = 0.5f;
    [Tooltip("Screen Y position (0–1) for the pan camera")]
    [SerializeField] private float panCamScreenY = 0.5f;

    [Header("Zone Follow Smoothing")]
    [SerializeField] private float zoneTargetSmoothSpeed = 10f;

    [Header("Falling Y-Damping Settings")]
    [SerializeField] private float fallPanAmount = 0.1f;
    [SerializeField] private float fallYPanTime = 0.35f;
    [Tooltip("Velocity threshold to trigger fall damping")]
    [SerializeField] private float fallSpeedThreshold = -15f;

    [Header("Falling Offset Settings")]
    [SerializeField] private float fallOffsetY = -3f;
    [SerializeField] private float fallOffsetLerpTime = 0.15f;
    [Tooltip("Horizontal look-ahead when moving")]
    [SerializeField] private float moveOffsetX = 7f;

    [Header("Blend Settings")]
    [Tooltip("Ease duration between normal zones")]
    [SerializeField] private float zoneBlendTime = 0.4f;
    [Tooltip("Ease duration into/out of pan zones")]
    [SerializeField] private float panBlendTime = 0.6f;
    [Tooltip("Default blend for other transitions")]
    [SerializeField] private float defaultBlendTime = 0.3f;

    private CinemachineBrain brain;
    private Transform player;
    private Rigidbody2D playerRB;

    private Vector2 defaultOffset;
    private float normYdamping;
    private float defaultVertYDamping;
    private Vector2 defaultVertOffset;

    private bool initialized;
    private bool isPanning;
    public bool IsPanning => isPanning;

    [SerializeField] private float panSmoothTime = 0.5f;

    private CameraZone currentZone;
    private readonly List<CameraZone> activeZones = new List<CameraZone>();
    private Vector3 zoneTargetVelocity;

    public CameraZone CurrentZone => activeZones.Count > 0 ? activeZones[^1] : null;

    private void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);
        else
            instance = this;

        brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    private IEnumerator Start()
    {
        yield return null;
        while (PlayerManager.instance == null ||
               PlayerUtils.GetPlayerSafe() == null ||
               !PlayerManager.instance.player.gameObject.activeInHierarchy)
            yield return null;

        player = PlayerUtils.GetPlayerSafe().transform;
        playerRB = PlayerUtils.GetPlayerSafe().GetComponent<Rigidbody2D>();

        EnableCameras();
        SetupCameras();

        SnapTargetsToBoundaryCenter();

        initialized = true;
        yield return new WaitForSeconds(0.05f);
        RestorePlayerCamDamping();
    }

    private void SnapTargetsToBoundaryCenter()
    {
        // world‐center of the main camera:
        var camPos = Camera.main.transform.position;
        camPos.z = -10f;

        if (horizontalTarget != null) horizontalTarget.position = camPos;
        if (verticalTarget != null) verticalTarget.position = camPos;
        if (fixedTarget != null) fixedTarget.position = camPos;
        if (panTarget != null) panTarget.position = camPos;
        if (cutsceneCamTarget != null) cutsceneCamTarget.position = camPos;
    }

    private void EnableCameras()
    {
        playerCam.enabled = true;
        fixedCam.enabled = true;
        horizontalCam.enabled = true;
        verticalCam.enabled = true;
        panCam.enabled = true;
    }

    private void SetupCameras()
    {
        // Player camera
        SetupCamera(playerCam, player, 20,
            playerCamXDamping, playerCamYDamping,
            playerCamDeadZoneWidth, playerCamDeadZoneHeight,
            playerCamSoftZoneWidth, playerCamSoftZoneHeight,
            playerCamScreenX, playerCamScreenY);

        // tracked offset & fall defaults
        var pt = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        pt.m_TrackedObjectOffset += new Vector3(0f, playerCamOffsetY, 0f);
        normYdamping = pt.m_YDamping;
        defaultOffset = pt.m_TrackedObjectOffset;

        // Horizontal-only
        SetupCamera(horizontalCam, horizontalTarget, 10,
            horizontalCamXDamping, horizontalCamYDamping,
            horizontalCamDeadZoneWidth, horizontalCamDeadZoneHeight,
            horizontalCamSoftZoneWidth, horizontalCamSoftZoneHeight,
            horizontalCamScreenX, horizontalCamScreenY);

        // Vertical-only
        SetupCamera(verticalCam, verticalTarget, 5,
            verticalCamXDamping, verticalCamYDamping,
            verticalCamDeadZoneWidth, verticalCamDeadZoneHeight,
            verticalCamSoftZoneWidth, verticalCamSoftZoneHeight,
            verticalCamScreenX, verticalCamScreenY);
        var vt = verticalCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        defaultVertYDamping = vt.m_YDamping;
        defaultVertOffset = vt.m_TrackedObjectOffset;

        // Fixed (no transposer override)
        SetupFixedCam();

        // Pan camera
        SetupCamera(panCam, panTarget, 5,
            panCamXDamping, panCamYDamping,
            panCamDeadZoneWidth, panCamDeadZoneHeight,
            panCamSoftZoneWidth, panCamSoftZoneHeight,
            panCamScreenX, panCamScreenY);
    }

    private void SetupCamera(
        CinemachineVirtualCamera cam,
        Transform target,
        int priority,
        float xD, float yD,
        float deadW, float deadH,
        float softW, float softH,
        float screenX, float screenY
    )
    {
        if (cam == null || target == null) return;
        cam.Follow = target;
        cam.Priority = priority;

        var t = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (t != null)
        {
            t.m_XDamping = xD;
            t.m_YDamping = yD;
            t.m_DeadZoneWidth = deadW;
            t.m_DeadZoneHeight = deadH;
            t.m_SoftZoneWidth = softW;
            t.m_SoftZoneHeight = softH;
            t.m_ScreenX = screenX;
            t.m_ScreenY = screenY;
        }

        cam.PreviousStateIsValid = false;
        cam.OnTargetObjectWarped(target, Vector3.zero);
    }

    private void SetupFixedCam()
    {
        if (fixedCam == null || fixedTarget == null) return;
        fixedCam.Follow = fixedTarget;
        fixedCam.Priority = 5;
    }

    private void RestorePlayerCamDamping()
    {
        if (playerCam == null) return;
        var t = playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (t != null)
        {
            t.m_XDamping = playerCamXDamping;
            t.m_YDamping = playerCamYDamping;
        }
    }

    public void PrepareForCutscene(Vector3 spawnPosition)
    {
        EnableCameras();
        if (cutsceneCamTarget == null || playerCam == null) return;

        cutsceneCamTarget.position = new Vector3(spawnPosition.x, spawnPosition.y, -10f);
        playerCam.Follow = cutsceneCamTarget;
        RestorePlayerCamDamping();
        ForceSnap();
        playerCam.Priority = 20;
    }


    public void ForceSnap()
    {
        if (playerCam != null && playerCam.Follow != null)
        {
            playerCam.PreviousStateIsValid = false;
            playerCam.OnTargetObjectWarped(playerCam.Follow, Vector3.zero);
        }
    }

    public void StartPan(PanDirection direction, float distance, float duration, Vector2 zoneCenter)
    {
        // 1) Snap the panTarget immediately to the final pan position
        Vector3 dirVec = direction switch
        {
            PanDirection.Up => Vector3.up,
            PanDirection.Down => Vector3.down,
            PanDirection.Left => Vector3.left,
            PanDirection.Right => Vector3.right,
            _ => Vector3.zero
        };
        Vector3 endPos = new Vector3(zoneCenter.x, zoneCenter.y, -10f) + dirVec * distance;
        panTarget.position = endPos;

        // 2) Immediately switch to the pan camera (your zone script's delay has already elapsed)
        SetBrainBlend(panBlendTime);
        SetPriority(panCam, 30);
        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        isPanning = true;

        // 3) Restore your default blend after the pan blend duration
        StartCoroutine(RestoreDefaultBlend(panBlendTime));
    }

    public void StopPan()
    {
        // bail out entirely if this component is disabled/inactive
        if (!this.isActiveAndEnabled)
            return;

        // only blend back if we actually switched in
        if (!isPanning)
            return;

        isPanning = false;
        SetBrainBlend(panBlendTime);
        SetPriority(panCam, 5);

        if (currentZone != null)
            ApplyZone(currentZone);
        else
            ResetToFreeFollow();

        // this one was already guarded before, and now the entire method is protected
        StartCoroutine(RestoreDefaultBlend(panBlendTime));
    }

    public void ApplyZone(CameraZone zone)
    {
        if (!initialized || player == null || zone == null || isPanning) return;
        SetBrainBlend(zoneBlendTime);
        if (!activeZones.Contains(zone)) activeZones.Add(zone);
        currentZone = zone;
        SwitchZone(currentZone);
        StartCoroutine(RestoreDefaultBlend(zoneBlendTime));
    }

    public void ExitZone(CameraZone zone)
    {
        if (!initialized || isPanning) return;
        SetBrainBlend(zoneBlendTime);
        if (activeZones.Remove(zone)) PruneInactiveZones();
        if (activeZones.Count > 0) currentZone = activeZones[^1];
        else currentZone = null;
        if (currentZone != null) SwitchZone(currentZone);
        else ResetToFreeFollow();
        StartCoroutine(RestoreDefaultBlend(zoneBlendTime));
    }

    private void PruneInactiveZones()
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            var z = activeZones[i];
            var col = z.GetComponent<Collider2D>();
            if (col == null || !col.OverlapPoint(player.position))
                activeZones.RemoveAt(i);
        }
    }

    private void SwitchZone(CameraZone zone)
    {
        // reset priorities
        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        switch (zone.mode)
        {
            case CameraZone.ZoneMode.Fixed:
                var fc = zone.GetFixedCenter();
                fixedTarget.position = new Vector3(fc.x, fc.y, -10f);
                SetPriority(fixedCam, 20);
                break;

            case CameraZone.ZoneMode.FreeFollow:
                ResetToFreeFollow();
                break;

            case CameraZone.ZoneMode.HorizontalOnly:
                horizontalTarget.position = new Vector3(player.position.x, zone.GetCenterY(), -10f);
                SetPriority(horizontalCam, 20);
                break;

            case CameraZone.ZoneMode.VerticalOnly:
                verticalTarget.position = new Vector3(zone.GetFixedCenter().x, player.position.y, -10f);
                SetPriority(verticalCam, 20);
                break;
        }
    }

    public void ResetToFreeFollow()
    {
        currentZone = null;
        SetupCamera(playerCam, player, 20,
            playerCamXDamping, playerCamYDamping,
            playerCamDeadZoneWidth, playerCamDeadZoneHeight,
            playerCamSoftZoneWidth, playerCamSoftZoneHeight,
            playerCamScreenX, playerCamScreenY);
        SetPriority(playerCam, 20);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Move zone follow targets for horizontal/vertical zones
        if (horizontalCam.Priority == 20 && currentZone != null)
        {
            Vector3 goal = new Vector3(player.position.x, currentZone.GetCenterY(), -10f);
            horizontalTarget.position = Vector3.SmoothDamp(
                horizontalTarget.position,
                goal,
                ref zoneTargetVelocity,
                1f / zoneTargetSmoothSpeed
            );
        }
        if (verticalCam.Priority == 20 && currentZone != null)
        {
            Vector3 goal = new Vector3(currentZone.GetFixedCenter().x, player.position.y, -10f);
            verticalTarget.position = Vector3.SmoothDamp(
                verticalTarget.position,
                goal,
                ref zoneTargetVelocity,
                1f / zoneTargetSmoothSpeed
            );
        }

        // Apply dynamic fall-follow offsets for FreeFollow and Vertical cams
        HandleFallFollow(playerCam, normYdamping, defaultOffset);
        HandleFallFollow(verticalCam, defaultVertYDamping, defaultVertOffset);

    }


    private void HandleFallFollow(CinemachineVirtualCamera cam, float baseYDamp, Vector2 baseOffset)
    {
        if (cam == null || cam.Priority != 20) return;
        var t = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
        bool falling = playerRB.velocity.y < fallSpeedThreshold;

        t.m_YDamping = Mathf.Lerp(t.m_YDamping, falling ? fallPanAmount : baseYDamp, Time.deltaTime / fallYPanTime);
        float lookY = falling ? fallOffsetY : baseOffset.y;
        t.m_TrackedObjectOffset = Vector2.Lerp(t.m_TrackedObjectOffset, new Vector2(baseOffset.x, lookY), Time.deltaTime / fallOffsetLerpTime);
    }

    private void SetBrainBlend(float blendTime)
    {
        if (brain != null)
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, blendTime);
    }

    private IEnumerator RestoreDefaultBlend(float delay)
    {
        yield return new WaitForSeconds(delay + 0.01f);
        SetBrainBlend(defaultBlendTime);
    }

    private void SetPriority(CinemachineVirtualCamera cam, int priority)
    {
        if (cam != null)
            cam.Priority = priority;
    }
}

public enum PanDirection { Up, Down, Left, Right }
