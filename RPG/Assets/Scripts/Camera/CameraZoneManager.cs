using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraZoneManager : MonoBehaviour
{
    public static CameraZoneManager instance;

    [Header("Cinemachine Virtual Cameras")]
    public CinemachineCamera playerCam;
    public CinemachineCamera fixedCam;
    public CinemachineCamera horizontalCam;
    public CinemachineCamera verticalCam;
    public CinemachineCamera panCam;

    [Header("Shared Follow Targets")]
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

    private CinemachineConfiner2D fixedConfiner;
    private float defaultScreenX, defaultScreenY;

    private bool initialized;
    private bool isPanning;
    public bool IsPanning => isPanning;

    [SerializeField] private float panSmoothTime = 0.5f;

    private CameraZone currentZone;
    private readonly List<CameraZone> activeZones = new List<CameraZone>();
    private Vector3 zoneTargetVelocity;

    private Transform cutsceneSubject;
    public void SetCutsceneSubject(Transform t) { cutsceneSubject = t; }
    private Transform CamSubject => cutsceneSubject != null ? cutsceneSubject : player;

    private bool freezeCamera = false;

    private Vector3 fixedTargetGoal;
    private Vector3 fixedTargetVel;
    private bool fixedLerpActive;

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
        freezeCamera = true;
        if (brain != null)
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut, 0f);

        Physics2D.SyncTransforms();

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

        var subject = player;
        if (subject != null)
        {
            CameraZone snapZone = null;
            var hits = Physics2D.OverlapPointAll(subject.position);
            for (int i = 0; i < hits.Length; i++)
            {
                var z = hits[i].GetComponent<CameraZone>();
                if (z != null) { snapZone = z; break; }
            }

            if (snapZone != null)
            {
                activeZones.Clear();
                currentZone = snapZone;
                activeZones.Add(snapZone);

                SwitchZone(currentZone);

                CinemachineCamera active = null;
                if (fixedCam != null && fixedCam.Priority.Value == 20) active = fixedCam;
                else if (horizontalCam != null && horizontalCam.Priority.Value == 20) active = horizontalCam;
                else if (verticalCam != null && verticalCam.Priority.Value == 20) active = verticalCam;
                else active = playerCam;

                Transform follow = (active != null) ? active.Follow : null;

                if (active != null && follow != null)
                {
                    active.PreviousStateIsValid = false;
                    active.OnTargetObjectWarped(follow, Vector3.zero);
                }

                if (active == fixedCam)
                {
                    // Snap instantly to fixed goal on scene load
                    fixedCam.transform.position = fixedTargetGoal;
                    fixedCam.PreviousStateIsValid = false;
                    fixedLerpActive = false;
                }
            }
        }
        SetBrainBlend(defaultBlendTime);
        freezeCamera = false;
        yield return new WaitForSeconds(0.05f);
        RestorePlayerCamDamping();
    }


    private void SnapTargetsToBoundaryCenter()
    {
        // world‐center of the main camera:
        var camPos = Camera.main.transform.position;
        camPos.z = -10f;
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

        CacheCameraComponents();
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
        var pt = playerCam.GetComponent<CinemachinePositionComposer>();
        if (pt != null)
        {
            pt.TargetOffset += new Vector3(0f, playerCamOffsetY, 0f);
            normYdamping = pt.Damping.y;
            defaultOffset = pt.TargetOffset;
            var c = pt.Composition;
            defaultScreenX = c.ScreenPosition.x;
            defaultScreenY = c.ScreenPosition.y;
        }

        // Horizontal-only
        SetupCamera(horizontalCam, horizontalCam != null ? horizontalCam.transform : null, 10,
            horizontalCamXDamping, horizontalCamYDamping,
            horizontalCamDeadZoneWidth, horizontalCamDeadZoneHeight,
            horizontalCamSoftZoneWidth, horizontalCamSoftZoneHeight,
            horizontalCamScreenX, horizontalCamScreenY);
        if (horizontalCam != null) horizontalCam.Follow = null;

        // Vertical-only
        SetupCamera(verticalCam, verticalCam != null ? verticalCam.transform : null, 5,
            verticalCamXDamping, verticalCamYDamping,
            verticalCamDeadZoneWidth, verticalCamDeadZoneHeight,
            verticalCamSoftZoneWidth, verticalCamSoftZoneHeight,
            verticalCamScreenX, verticalCamScreenY);
        var vt = verticalCam.GetComponent<CinemachinePositionComposer>();
        if (vt != null)
        {
            defaultVertYDamping = vt.Damping.y;
            defaultVertOffset = vt.TargetOffset;
        }

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
    CinemachineCamera cam,
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
        cam.Priority = new PrioritySettings { Enabled = true, Value = priority };

        var t = cam.GetComponent<CinemachinePositionComposer>();
        if (t != null)
        {
            // Damping
            var d = t.Damping; d.x = xD; d.y = yD; t.Damping = d;

            // CM3: screen/dead/soft live under Composition
            var c = t.Composition;

            // Dead zone
            var dz = c.DeadZone;
            dz.Enabled = (deadW != 0f || deadH != 0f);
            dz.Size = new Vector2(deadW, deadH);
            c.DeadZone = dz;

            // Soft zone → use HardLimits in CM3
            var hl = c.HardLimits;
            hl.Enabled = (softW != 0f || softH != 0f);
            hl.Size = new Vector2(softW, softH);
            c.HardLimits = hl;

            // Screen position
            c.ScreenPosition = new Vector2(screenX, screenY);

            t.Composition = c;
        }

        cam.PreviousStateIsValid = false;
        cam.OnTargetObjectWarped(target, Vector3.zero);
    }


    private void SetupFixedCam()
    {
        if (fixedCam == null) return;

        // We will NOT use Follow for fixedCam; it will be driven by transform.
        fixedCam.Follow = null;
        fixedCam.Priority = new PrioritySettings { Enabled = true, Value = 5 };

        // Ensure there is no body/composer damping to push the view
        var ft = fixedCam.GetComponent<CinemachinePositionComposer>();
        if (ft != null)
        {
            var d = ft.Damping; d.x = 0f; d.y = 0f; ft.Damping = d;

            // CM3: zones & screen live under Composition
            var c = ft.Composition;

            var dz = c.DeadZone; dz.Enabled = false; dz.Size = Vector2.zero; c.DeadZone = dz;
            var hl = c.HardLimits; hl.Enabled = false; hl.Size = Vector2.zero; c.HardLimits = hl;

            c.ScreenPosition = new Vector2(0.5f, 0.5f);
            ft.Composition = c;
        }

        var comp = fixedCam.GetComponent<CinemachineRotationComposer>();
        if (comp != null)
        {
            comp.Damping = Vector2.zero;

            // Same CM3 Composition pattern
            var rc = comp.Composition;
            var dz = rc.DeadZone; dz.Enabled = false; dz.Size = Vector2.zero; rc.DeadZone = dz;
            rc.ScreenPosition = new Vector2(0.5f, 0.5f);
            comp.Composition = rc;
        }
    }


    private void RestorePlayerCamDamping()
    {
        if (playerCam == null) return;
        var t = playerCam.GetComponent<CinemachinePositionComposer>();
        if (t != null)
        {
            var d = t.Damping; d.x = playerCamXDamping; d.y = playerCamYDamping; t.Damping = d;
        }
    }

    public void PrepareForCutscene(Vector3 spawnPosition)
    {
        EnableCameras();
        if (playerCam == null) return;

        freezeCamera = true;

        // --- Find a zone at the spawn point (if any) ---
        CameraZone zoneAtSpawn = null;
        var hits = Physics2D.OverlapPointAll(spawnPosition);
        for (int i = 0; i < hits.Length; i++)
        {
            var z = hits[i].GetComponent<CameraZone>();
            if (z != null) { zoneAtSpawn = z; break; }
        }

        // Save & switch to a hard CUT so we don't see any blend before the fade
        CinemachineBlendDefinition savedBlend = default;
        bool haveBrain = (brain != null);
        if (haveBrain)
        {
            savedBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut, 0f);
        }

        // Reset vcam priorities
        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        CinemachineCamera active = null;

        // --- If the spawn is inside a non-FreeFollow zone, use that zone camera ---
        if (zoneAtSpawn != null && zoneAtSpawn.mode != CameraZone.ZoneMode.FreeFollow)
        {
            currentZone = zoneAtSpawn;
            activeZones.Clear();
            activeZones.Add(zoneAtSpawn);

            var zt = zoneAtSpawn.zoneTarget;

            switch (zoneAtSpawn.mode)
            {
                case CameraZone.ZoneMode.Fixed:
                    {
                        if (fixedCam != null)
                        {
                            // No confiner push, no follow, place at exact center
                            var conf = fixedCam.GetComponent<CinemachineConfiner2D>();
                            if (conf != null) conf.enabled = false;

                            var fc = zoneAtSpawn.GetFixedCenter();
                            var p = new Vector3(fc.x, fc.y, -10f);

                            fixedCam.Follow = null;
                            fixedCam.transform.position = p;

                            // lock pose for cutscene; no lerp
                            fixedTargetGoal = p;
                            fixedLerpActive = false;

                            SetPriority(fixedCam, 20);
                            active = fixedCam;
                        }
                        break;
                    }
                case CameraZone.ZoneMode.HorizontalOnly:
                    {
                        if (zt != null) zt.position = new Vector3(spawnPosition.x, zoneAtSpawn.GetCenterY(), -10f);
                        if (horizontalCam != null) horizontalCam.Follow = zt;
                        SetPriority(horizontalCam, 20);
                        active = horizontalCam;
                        break;
                    }

                case CameraZone.ZoneMode.VerticalOnly:
                    {
                        if (zt != null) zt.position = new Vector3(zoneAtSpawn.GetFixedCenter().x, spawnPosition.y, -10f);
                        if (verticalCam != null) verticalCam.Follow = zt;
                        SetPriority(verticalCam, 20);
                        active = verticalCam;
                        break;
                    }
            }
        }
        else
        {
            // --- No zone (or FreeFollow): use the playerCam with the cutscene target ---
            if (cutsceneCamTarget == null)
            {
                if (haveBrain) brain.DefaultBlend = savedBlend; freezeCamera = false; return;
            }

            cutsceneCamTarget.position = new Vector3(spawnPosition.x, spawnPosition.y, -10f);
            playerCam.Follow = cutsceneCamTarget;

            // Use the same gameplay damping you already configure
            RestorePlayerCamDamping();

            SetPriority(playerCam, 20);
            active = playerCam;
            cutsceneSubject = cutsceneCamTarget;
        }

        // --- Force an immediate snap (no easing) so the camera is correct before fade starts ---
        if (active != null && active.Follow != null)
        {
            active.PreviousStateIsValid = false;
            active.OnTargetObjectWarped(active.Follow, Vector3.zero);
        }
        else if (active != null)
        {
            active.PreviousStateIsValid = false;
        }

        // Restore your normal blend for later transitions
        if (haveBrain)
            brain.DefaultBlend = savedBlend;
        freezeCamera = false;
    }

    public void FreezeForSceneExit()
    {
        EnableCameras();

        freezeCamera = true;

        if (cutsceneSubject == null && player != null)
            cutsceneSubject = player;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 currentPose = cam.transform.position;
        currentPose.z = -10f;
        float currentOrthoSize = cam.orthographic ? cam.orthographicSize : 0f;

        if (fixedCam == null) return;

        var lens = fixedCam.Lens;
        lens.OrthographicSize = currentOrthoSize;
        fixedCam.Lens = lens;

        fixedCam.Follow = null;
        fixedCam.transform.position = currentPose;

        var ft = fixedCam.GetComponent<CinemachinePositionComposer>();
        if (ft != null)
        {
            var d = ft.Damping; d.x = 0f; d.y = 0f; ft.Damping = d;

            // CM3: zones & screen live under Composition
            var c = ft.Composition;
            var dz = c.DeadZone; dz.Enabled = false; dz.Size = Vector2.zero; c.DeadZone = dz;
            var hl = c.HardLimits; hl.Enabled = false; hl.Size = Vector2.zero; c.HardLimits = hl;
            c.ScreenPosition = new Vector2(0.5f, 0.5f);
            ft.Composition = c;
        }

        var comp = fixedCam.GetComponent<CinemachineRotationComposer>();
        if (comp != null)
        {
            comp.Damping = Vector2.zero;

            // CM3: also via Composition
            var rc = comp.Composition;
            var dz = rc.DeadZone; dz.Enabled = false; dz.Size = Vector2.zero; rc.DeadZone = dz;
            rc.ScreenPosition = new Vector2(0.5f, 0.5f);
            comp.Composition = rc;
        }

        var conf = fixedCam.GetComponent<CinemachineConfiner2D>();
        bool confWasEnabled = false;
        if (conf != null)
        {
            confWasEnabled = conf.enabled;
            conf.enabled = false;
        }

        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(panCam, 5);
        SetPriority(fixedCam, 100);

        CinemachineBlendDefinition savedBlend = default;
        if (brain != null)
        {
            savedBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut, 0f);
        }

        fixedCam.PreviousStateIsValid = false;

        if (brain != null)
            brain.DefaultBlend = savedBlend;
    }


    public void ForceSnap()
    {
        CinemachineCamera active = null;

        if (fixedCam != null && fixedCam.Priority.Value == 20) active = fixedCam;
        else if (horizontalCam != null && horizontalCam.Priority.Value == 20) active = horizontalCam;
        else if (verticalCam != null && verticalCam.Priority.Value == 20) active = verticalCam;
        else if (playerCam != null) active = playerCam;

        var follow = (active != null) ? active.Follow : null;

        if (active != null && follow != null)
        {
            active.PreviousStateIsValid = false;
            active.OnTargetObjectWarped(follow, Vector3.zero);
        }
    }

    public void StartPan(PanDirection direction, float distance, float duration, Vector2 zoneCenter)
    {
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

        SetBrainBlend(panBlendTime);
        SetPriority(panCam, 30);
        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        isPanning = true;

        StartCoroutine(RestoreDefaultBlend(panBlendTime));
    }

    public void StopPan()
    {
        if (!this.isActiveAndEnabled)
            return;

        if (!isPanning)
            return;

        isPanning = false;
        SetBrainBlend(panBlendTime);
        SetPriority(panCam, 5);

        if (currentZone != null)
            ApplyZone(currentZone);
        else
            ResetToFreeFollow();

        StartCoroutine(RestoreDefaultBlend(panBlendTime));
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void ApplyZone(CameraZone zone)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !gameObject.activeInHierarchy) return;
        if (!initialized || player == null || zone == null || isPanning) return;

        zoneTargetVelocity = Vector3.zero;

        var prevZone = currentZone;
        var prevMode = (prevZone != null) ? prevZone.mode : CameraZone.ZoneMode.FreeFollow;

        SetBrainBlend(zoneBlendTime);
        if (!activeZones.Contains(zone)) activeZones.Add(zone);
        currentZone = zone;

        var zt = zone.zoneTarget;

        switch (zone.mode)
        {
            case CameraZone.ZoneMode.HorizontalOnly:
                if (zt != null)
                {
                    zt.position = new Vector3(CamSubject.position.x, zone.GetCenterY(), -10f);
                    if (horizontalCam != null) horizontalCam.Follow = zt;
                }
                break;

            case CameraZone.ZoneMode.VerticalOnly:
                if (zt != null)
                {
                    zt.position = new Vector3(zone.GetFixedCenter().x, CamSubject.position.y, -10f);
                    if (verticalCam != null) verticalCam.Follow = zt;
                }
                break;

            case CameraZone.ZoneMode.Fixed:
                if (fixedCam != null)
                {
                    var fc = zone.GetFixedCenter();
                    fixedTargetGoal = new Vector3(fc.x, fc.y, -10f);
                    fixedLerpActive = true;

                    fixedCam.Follow = null;
                }
                break;
        }

        // Copy playerCam's screen position into the active axis cam
        var pt = playerCam != null ? playerCam.GetComponent<CinemachinePositionComposer>() : null;
        if (pt != null)
        {
            var srcComp = pt.Composition; // CM3: ScreenPosition is under Composition

            if (zone.mode == CameraZone.ZoneMode.HorizontalOnly && horizontalCam != null)
            {
                var ht = horizontalCam.GetComponent<CinemachinePositionComposer>();
                if (ht != null)
                {
                    var hc = ht.Composition;
                    hc.ScreenPosition = srcComp.ScreenPosition;
                    ht.Composition = hc;
                }
            }
            else if (zone.mode == CameraZone.ZoneMode.VerticalOnly && verticalCam != null)
            {
                var vtc = verticalCam.GetComponent<CinemachinePositionComposer>();
                if (vtc != null)
                {
                    var vc = vtc.Composition;
                    vc.ScreenPosition = srcComp.ScreenPosition;
                    vtc.Composition = vc;
                }
            }
        }

        if (zone.mode == CameraZone.ZoneMode.FreeFollow && playerCam != null)
        {
            var t = playerCam.GetComponent<CinemachinePositionComposer>();
            if (t != null)
            {
                // Tracked offset
                t.TargetOffset = zone.overrideTrackedOffset ? zone.trackedOffset : defaultOffset;

                // Screen position via Composition
                var c = t.Composition;
                c.ScreenPosition = zone.overrideScreenXY
                    ? new Vector2(zone.screenX, zone.screenY)
                    : new Vector2(defaultScreenX, defaultScreenY);
                t.Composition = c;
            }
        }
        else
        {
            RestorePlayerCamFramingDefaults();
        }

        SwitchZone(currentZone);

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
            StartCoroutine(RestoreDefaultBlend(zoneBlendTime));
    }


    private void RestorePlayerCamFramingDefaults()
    {
        if (playerCam == null) return;
        var t = playerCam.GetComponent<CinemachinePositionComposer>();
        if (t == null) return;

        t.TargetOffset = defaultOffset;

        // CM3: ScreenPosition lives under Composition
        var c = t.Composition;
        c.ScreenPosition = new Vector2(defaultScreenX, defaultScreenY);
        t.Composition = c;
    }


    public void ExitZone(CameraZone zone)
    {
        if (!initialized || isPanning) return;
        if (!Application.isPlaying || !isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        zoneTargetVelocity = Vector3.zero;

        SetBrainBlend(zoneBlendTime);
        if (activeZones.Remove(zone)) PruneInactiveZones();
        currentZone = (activeZones.Count > 0) ? activeZones[^1] : null;

        if (currentZone != null)
        {
            var zt = currentZone.zoneTarget;

            switch (currentZone.mode)
            {
                case CameraZone.ZoneMode.HorizontalOnly:
                    if (zt != null) zt.position = new Vector3(CamSubject.position.x, currentZone.GetCenterY(), -10f);
                    if (horizontalCam != null) horizontalCam.Follow = zt;
                    break;

                case CameraZone.ZoneMode.VerticalOnly:
                    if (zt != null) zt.position = new Vector3(currentZone.GetFixedCenter().x, CamSubject.position.y, -10f);
                    if (verticalCam != null) verticalCam.Follow = zt;
                    break;

                case CameraZone.ZoneMode.Fixed:
                    break;
            }

            SwitchZone(currentZone);
        }
        else
        {
            ResetToFreeFollow();
        }

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
            StartCoroutine(RestoreDefaultBlend(zoneBlendTime));
    }

    private void PruneInactiveZones()
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            var z = activeZones[i];
            var col = z.GetComponent<Collider2D>();
            if (col == null || !col.OverlapPoint(CamSubject.position))
                activeZones.RemoveAt(i);
        }
    }

    private void SwitchZone(CameraZone zone)
    {
        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        var zt = zone.zoneTarget;

        switch (zone.mode)
        {
            case CameraZone.ZoneMode.Fixed:
                SetConfinerEnabled(fixedCam, false);

                if (fixedCam != null)
                {
                    var fc = zone.GetFixedCenter();
                    fixedTargetGoal = new Vector3(fc.x, fc.y, -10f);

                    var currentPose = (Camera.main != null) ? Camera.main.transform.position : fixedTargetGoal;
                    currentPose.z = -10f;

                    fixedCam.Follow = null;
                    fixedCam.transform.position = currentPose;
                    fixedCam.PreviousStateIsValid = false;

                    fixedLerpActive = true;
                }

                SetPriority(fixedCam, 20);
                break;

            case CameraZone.ZoneMode.FreeFollow:
                SetConfinerEnabled(playerCam, true);
                ResetToFreeFollow();
                break;

            case CameraZone.ZoneMode.HorizontalOnly:
                SetConfinerEnabled(horizontalCam, true);
                if (zt != null)
                {
                    Transform prevFollow = (horizontalCam != null) ? horizontalCam.Follow : null;
                    if (prevFollow != null && prevFollow != zt)
                    {
                        zt.position = prevFollow.position;
                    }
                    else if (prevFollow == null)
                    {
                        zt.position = new Vector3(CamSubject.position.x, zone.GetCenterY(), -10f);
                    }
                    if (horizontalCam != null) horizontalCam.Follow = zt;
                }
                SetPriority(horizontalCam, 20);
                break;

            case CameraZone.ZoneMode.VerticalOnly:
                SetConfinerEnabled(verticalCam, true);
                if (zt != null)
                {
                    Transform prevFollow = (verticalCam != null) ? verticalCam.Follow : null;
                    if (prevFollow != null && prevFollow != zt)
                    {
                        zt.position = prevFollow.position;
                    }
                    else if (prevFollow == null)
                    {
                        zt.position = new Vector3(zone.GetFixedCenter().x, CamSubject.position.y, -10f);
                    }
                    if (verticalCam != null) verticalCam.Follow = zt;
                }
                SetPriority(verticalCam, 20);
                break;
        }
    }

    public void SnapToZoneAtPoint(Vector2 point)
    {
        CameraZone[] zones = FindObjectsByType<CameraZone>(FindObjectsSortMode.None);
        CameraZone found = null;

        foreach (var z in zones)
        {
            var col = z.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(point))
            {
                found = z;
                break;
            }
        }

        SetPriority(playerCam, 5);
        SetPriority(horizontalCam, 5);
        SetPriority(verticalCam, 5);
        SetPriority(fixedCam, 5);

        currentZone = null;
        activeZones.Clear();

        if (found == null)
            return;

        currentZone = found;
        activeZones.Add(found);

        var zt = found.zoneTarget;

        switch (found.mode)
        {
            case CameraZone.ZoneMode.Fixed:
                {
                    if (zt != null)
                    {
                        var fc = found.GetFixedCenter();
                        zt.position = new Vector3(fc.x, fc.y, -10f);
                    }
                    if (fixedCam != null) fixedCam.Follow = zt;
                    SetPriority(fixedCam, 20);
                    break;
                }
            case CameraZone.ZoneMode.HorizontalOnly:
                {
                    if (zt != null) zt.position = new Vector3(point.x, found.GetCenterY(), -10f);
                    if (horizontalCam != null) horizontalCam.Follow = zt;
                    SetPriority(horizontalCam, 20);
                    break;
                }
            case CameraZone.ZoneMode.VerticalOnly:
                {
                    if (zt != null) zt.position = new Vector3(found.GetFixedCenter().x, point.y, -10f);
                    if (verticalCam != null) verticalCam.Follow = zt;
                    SetPriority(verticalCam, 20);
                    break;
                }
            case CameraZone.ZoneMode.FreeFollow:
            default:
                break;
        }
    }

    public void ClearCutsceneSubject()
    {
        if (playerCam != null && cutsceneCamTarget != null && player != null)
        {
            if (playerCam.Follow == cutsceneCamTarget)
            {
                playerCam.Follow = player;
                playerCam.PreviousStateIsValid = false;
                playerCam.OnTargetObjectWarped(player, Vector3.zero);
            }
        }

        cutsceneSubject = null;
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
        if (freezeCamera) return;
        if (player == null || !player.gameObject.activeInHierarchy) return;

        bool cutsceneActive = (cutsceneSubject != null);

        if (!cutsceneActive)
        {
            var cz = currentZone;

            if (cz != null)
            {
                // Horizontal-only zone: drive the ACTIVE vcam's Follow (not the zone directly)
                if (cz.mode == CameraZone.ZoneMode.HorizontalOnly &&
                    horizontalCam != null && horizontalCam.Priority.Value == 20 &&
                    horizontalCam.Follow != null)
                {
                    Transform follow = horizontalCam.Follow;
                    Vector3 goal = new Vector3(
                        CamSubject.position.x + cz.xBias,
                        cz.GetCenterY() + cz.yBias,
                        -10f
                    );
                    follow.position = Vector3.SmoothDamp(
                        follow.position,
                        goal,
                        ref zoneTargetVelocity,
                        1f / zoneTargetSmoothSpeed
                    );
                }

                // Vertical-only zone
                if (cz.mode == CameraZone.ZoneMode.VerticalOnly &&
                    verticalCam != null && verticalCam.Priority.Value == 20 &&
                    verticalCam.Follow != null)
                {
                    Transform follow = verticalCam.Follow;
                    Vector3 goal = new Vector3(
                        cz.GetFixedCenter().x + cz.xBias,
                        CamSubject.position.y + cz.yBias,
                        -10f
                    );
                    follow.position = Vector3.SmoothDamp(
                        follow.position,
                        goal,
                        ref zoneTargetVelocity,
                        1f / zoneTargetSmoothSpeed
                    );
                }

                if (cz != null &&
                    cz.mode == CameraZone.ZoneMode.Fixed &&
                    fixedCam != null && fixedCam.Priority.Value == 20)
                {
                    // We are in Fixed; fixedCam.Follow is intentionally null.
                    if (fixedLerpActive)
                    {
                        var pos = fixedCam.transform.position;
                        pos = Vector3.SmoothDamp(
                            pos,
                            fixedTargetGoal,
                            ref fixedTargetVel,
                            1f / zoneTargetSmoothSpeed
                        );
                        pos.z = -10f;
                        fixedCam.transform.position = pos;

                        // stop when close enough
                        if ((fixedCam.transform.position - fixedTargetGoal).sqrMagnitude < 0.0001f)
                            fixedLerpActive = false;
                    }
                }
            }
        }

        if (!cutsceneActive && playerRB != null)
        {
            HandleFallFollow(playerCam, normYdamping, defaultOffset);
            HandleFallFollow(verticalCam, defaultVertYDamping, defaultVertOffset);
        }
    }

    private void CacheCameraComponents()
    {
        if (fixedCam != null)
            fixedConfiner = fixedCam.GetComponent<CinemachineConfiner2D>();
    }

    private void SetConfinerEnabled(CinemachineCamera cam, bool enabled)
    {
        if (cam == null) return;
        var conf = cam.GetComponent<CinemachineConfiner2D>();
        if (conf != null) conf.enabled = enabled;
    }

    private void HandleFallFollow(CinemachineCamera cam, float baseYDamp, Vector2 baseOffset)
    {
        if (cam == null || cam.Priority.Value != 20) return;
        if (playerRB == null) return;
        var t = cam.GetComponent<CinemachinePositionComposer>();
        if (t == null) return;

        bool falling = playerRB.linearVelocity.y < fallSpeedThreshold;

        var d = t.Damping;
        d.y = Mathf.Lerp(d.y, falling ? fallPanAmount : baseYDamp, Time.deltaTime / fallYPanTime);
        t.Damping = d;

        float lookY = falling ? fallOffsetY : baseOffset.y;
        t.TargetOffset = Vector2.Lerp(t.TargetOffset, new Vector2(baseOffset.x, lookY), Time.deltaTime / fallOffsetLerpTime);
    }

    private void SetBrainBlend(float blendTime)
    {
        if (brain != null)
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);
    }

    private IEnumerator RestoreDefaultBlend(float delay)
    {
        yield return new WaitForSeconds(delay + 0.01f);
        SetBrainBlend(defaultBlendTime);
    }

    private void SetPriority(CinemachineCamera cam, int priority)
    {
        if (cam != null)
            cam.Priority = new PrioritySettings { Enabled = true, Value = priority };
    }
}

public enum PanDirection { Up, Down, Left, Right }
