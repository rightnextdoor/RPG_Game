using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private CinemachineVirtualCamera[] allVirtualCameras;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallYPanTime = 0.35f;
    public float fallSpeedYDampingChangeThreshold = -15f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private CinemachineVirtualCamera currentCamera;
    private CinemachineFramingTransposer framingTransposer;
    
    private Coroutine lerpYPanCoroutine;
    private Coroutine panCameraCoroutine;

    private float normYPanAmount;

    private Vector2 startingTrackedObjectOffset;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < allVirtualCameras.Length; i++)
        {
            if (allVirtualCameras[i].enabled)
            {
                currentCamera = allVirtualCameras[i];

                framingTransposer = currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            }
        }
        normYPanAmount = framingTransposer.m_YDamping;

        startingTrackedObjectOffset = framingTransposer.m_TrackedObjectOffset;
    }

    #region Lerp the Y Damping

    public void LerpYDamping(bool isPlayerFalling)
    {
        lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {     
        IsLerpingYDamping = true;

        float startDampAmount = framingTransposer.m_YDamping;
        float endDampAmount = 0f;

        if (isPlayerFalling)
        {
            endDampAmount = fallPanAmount;
            LerpedFromPlayerFalling = true;
        } else
        {
            endDampAmount = normYPanAmount;
        }

        float elapsedTime = 0f;
        while (elapsedTime < fallYPanTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount,(elapsedTime / fallYPanTime));
            framingTransposer.m_YDamping = lerpedPanAmount;

            yield return null;
        }

        IsLerpingYDamping = false;
    }

    #endregion

    #region Pan Camera

    public void PanCameraOnContact(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        if(currentCamera.isActiveAndEnabled)
            panCameraCoroutine = StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        Vector2 endPos = Vector2.zero;
        Vector2 startingPos = Vector2.zero;

        if (!panToStartingPos)
        {
            switch (panDirection)
            {
                case PanDirection.Up:
                    endPos = Vector2.up;
                    break;
                case PanDirection.Down:
                    endPos = Vector2.down;
                    break;
                case PanDirection.Left:
                    endPos = Vector2.right;
                    break;
                case PanDirection.Right:
                    endPos = Vector2.left;
                    break;
                default:
                    break;
            }
            endPos *= panDistance;
            startingPos = startingTrackedObjectOffset;
            endPos += startingPos;
        }
        else
        {
            startingPos = framingTransposer.m_TrackedObjectOffset;
            endPos = startingTrackedObjectOffset;
        }

        float elapsedTime = 0f;
        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;

            Vector3 panLerp = Vector3.Lerp(startingPos, endPos, (elapsedTime / panTime));
            framingTransposer.m_TrackedObjectOffset = panLerp;

            yield return null;
        }
    }

    #endregion

    #region Swap Cameras

    public void SwapCamera(CinemachineVirtualCamera cameraFromLeft, CinemachineVirtualCamera cameraFromRight, Vector2 triggerExitDirection)
    {
        //if the current camera is the camera on the left and our trigger exit direction was on the right
        if (currentCamera == cameraFromLeft && triggerExitDirection.x > 0f)
        {
            cameraFromRight.enabled = true;
            cameraFromLeft.enabled = false;

            currentCamera = cameraFromRight;
            framingTransposer = currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }
        else if (currentCamera == cameraFromRight && triggerExitDirection.x < 0f)
        {
            cameraFromLeft.enabled = true;
            cameraFromRight.enabled = false;

            currentCamera = cameraFromLeft;
            framingTransposer = currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }
    }

    #endregion
}
