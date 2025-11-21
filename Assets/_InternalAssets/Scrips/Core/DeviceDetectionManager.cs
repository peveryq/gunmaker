using UnityEngine;
using YG;

/// <summary>
/// Singleton manager for device detection using YG2 SDK.
/// Determines if the current device is mobile, tablet, or desktop.
/// </summary>
public class DeviceDetectionManager : MonoBehaviour
{
    public static DeviceDetectionManager Instance { get; private set; }
    
    [Header("Device Detection")]
    [SerializeField] private bool forceDesktop = false; // For testing in editor
    
    public enum DeviceType
    {
        Desktop,
        Mobile,
        Tablet
    }
    
    public DeviceType CurrentDeviceType { get; private set; } = DeviceType.Desktop;
    public bool IsMobile => CurrentDeviceType == DeviceType.Mobile;
    public bool IsTablet => CurrentDeviceType == DeviceType.Tablet;
    public bool IsMobileOrTablet => IsMobile || IsTablet;
    public bool IsDesktop => CurrentDeviceType == DeviceType.Desktop;
    
    public System.Action<DeviceType> OnDeviceTypeChanged;
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Move to root if parented (safety for DontDestroyOnLoad)
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DetectDevice();
    }
    
    private void Start()
    {
        // Subscribe to YG2 SDK ready event to re-detect device when SDK is available
        if (YG2.isSDKEnabled)
        {
            DetectDevice();
        }
        else
        {
            YG2.onGetSDKData += OnSDKReady;
        }
    }
    
    private void OnDestroy()
    {
        if (YG2.onGetSDKData != null)
        {
            YG2.onGetSDKData -= OnSDKReady;
        }
    }
    
    private void OnSDKReady()
    {
        DetectDevice();
        YG2.onGetSDKData -= OnSDKReady;
    }
    
    private void DetectDevice()
    {
        DeviceType previousType = CurrentDeviceType;
        
        // Force desktop for testing in editor
        if (forceDesktop && Application.isEditor)
        {
            CurrentDeviceType = DeviceType.Desktop;
            Debug.Log("DeviceDetectionManager: Forced desktop mode for testing");
        }
        else if (YG2.isSDKEnabled && YG2.envir != null)
        {
            // Use YG2 device detection
            if (YG2.envir.isMobile)
            {
                CurrentDeviceType = DeviceType.Mobile;
            }
            else if (YG2.envir.isTablet)
            {
                CurrentDeviceType = DeviceType.Tablet;
            }
            else
            {
                CurrentDeviceType = DeviceType.Desktop;
            }
            
            Debug.Log($"DeviceDetectionManager: Detected device type via YG2: {CurrentDeviceType} (deviceType: {YG2.envir.deviceType})");
        }
        else
        {
            // Fallback detection using Unity's SystemInfo
            if (Application.isMobilePlatform || SystemInfo.deviceType == UnityEngine.DeviceType.Handheld)
            {
                // Simple heuristic: assume smaller screens are phones, larger are tablets
                float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height) / Screen.dpi;
                
                if (screenDiagonal < 7.0f) // Less than 7 inches diagonal
                {
                    CurrentDeviceType = DeviceType.Mobile;
                }
                else
                {
                    CurrentDeviceType = DeviceType.Tablet;
                }
            }
            else
            {
                CurrentDeviceType = DeviceType.Desktop;
            }
            
            Debug.Log($"DeviceDetectionManager: Detected device type via fallback: {CurrentDeviceType}");
        }
        
        // Notify if device type changed
        if (previousType != CurrentDeviceType)
        {
            OnDeviceTypeChanged?.Invoke(CurrentDeviceType);
        }
    }
    
    /// <summary>
    /// Force re-detection of device type (useful for testing)
    /// </summary>
    public void RefreshDeviceDetection()
    {
        DetectDevice();
    }
    
    /// <summary>
    /// Override device type for testing purposes
    /// </summary>
    public void SetDeviceTypeForTesting(DeviceType deviceType)
    {
        if (Application.isEditor)
        {
            CurrentDeviceType = deviceType;
            OnDeviceTypeChanged?.Invoke(CurrentDeviceType);
            Debug.Log($"DeviceDetectionManager: Device type set to {deviceType} for testing");
        }
    }
}
