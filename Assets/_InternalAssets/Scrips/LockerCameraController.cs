using UnityEngine;
#if CINEMACHINE
using Cinemachine;
#endif

public class LockerCameraController : MonoBehaviour
{
#if CINEMACHINE
    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera lockerCamera;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 5;
#else
    [Header("Cinemachine")]
    [Tooltip("Cinemachine package not detected. Define the CINEMACHINE scripting symbol once the package is installed.")]
    [SerializeField] private bool cinemachineNotAvailable;
#endif

    public void EnterLockerView()
    {
#if CINEMACHINE
        if (lockerCamera != null)
        {
            lockerCamera.Priority = activePriority;
        }
#endif
    }

    public void ExitLockerView()
    {
#if CINEMACHINE
        if (lockerCamera != null)
        {
            lockerCamera.Priority = inactivePriority;
        }
#endif
    }

    private void OnDisable()
    {
        ExitLockerView();
    }
}

