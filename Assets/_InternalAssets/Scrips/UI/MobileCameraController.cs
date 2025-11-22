using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Handles mobile camera control with touch input filtering.
/// Prevents camera movement when touching UI elements or joystick areas.
/// </summary>
public class MobileCameraController : MonoBehaviour
{
    [Header("Touch Settings")]
    [SerializeField] private bool invertY = false;
    
    [Header("UI Exclusion Areas")]
    [Tooltip("UI areas where touch should not control camera (e.g., joystick, buttons)")]
    [SerializeField] private RectTransform[] exclusionAreas;
    
    [Header("Camera Control Area")]
    [Tooltip("If set, only touches within this area will control camera. If null, entire screen is used.")]
    [SerializeField] private RectTransform cameraControlArea;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for touch events")]
    [SerializeField] private bool enableDebugLogging = false;
    
    private FirstPersonController fpsController;
    private Camera playerCamera;
    private bool isMobileDevice = false;
    private bool isEnabled = false;
    
    // Touch tracking
    private Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();
    private int cameraControlTouchId = -1;
    
    private void Start()
    {
        // Find FPS controller
        fpsController = FindFirstObjectByType<FirstPersonController>();
        if (fpsController != null)
        {
            playerCamera = fpsController.GetComponentInChildren<Camera>();
        }
        
        // Check device type
        StartCoroutine(InitializeDeviceDetection());
    }
    
    private System.Collections.IEnumerator InitializeDeviceDetection()
    {
        // Wait for DeviceDetectionManager to be ready
        while (DeviceDetectionManager.Instance == null)
        {
            yield return null;
        }
        
        // Subscribe to device type changes
        DeviceDetectionManager.Instance.OnDeviceTypeChanged += OnDeviceTypeChanged;
        
        // Initialize based on current device type
        OnDeviceTypeChanged(DeviceDetectionManager.Instance.CurrentDeviceType);
    }
    
    private void OnDestroy()
    {
        if (DeviceDetectionManager.Instance != null)
        {
            DeviceDetectionManager.Instance.OnDeviceTypeChanged -= OnDeviceTypeChanged;
        }
    }
    
    private void OnDeviceTypeChanged(DeviceDetectionManager.DeviceType deviceType)
    {
        isMobileDevice = deviceType == DeviceDetectionManager.DeviceType.Mobile || 
                        deviceType == DeviceDetectionManager.DeviceType.Tablet;
        
        SetEnabled(isMobileDevice);
        
        // Enable external mouse input mode on FirstPersonController for mobile devices
        if (fpsController != null)
        {
            fpsController.SetUseExternalMouseInput(isMobileDevice);
        }
    }
    
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled && isMobileDevice;
    }
    
    private void Update()
    {
        if (!isEnabled || fpsController == null) return;
        
        HandleTouchInput();
    }
    
    private void HandleTouchInput()
    {
        // Process real touches on mobile devices
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            ProcessTouch(touch);
        }
        
        // Simulate touch with mouse for testing in editor
        #if UNITY_EDITOR
        if (Input.touchCount == 0) // Only if no real touches
        {
            SimulateMouseAsTouch();
        }
        #endif
        
        // Clean up ended touches
        CleanupEndedTouches();
    }
    
    private void ProcessTouch(Touch touch)
    {
        Vector2 screenPos = touch.position;
        
        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandleTouchBegan(touch.fingerId, screenPos);
                break;
                
            case TouchPhase.Moved:
                HandleTouchMoved(touch.fingerId, screenPos, touch.deltaPosition);
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleTouchEnded(touch.fingerId);
                break;
        }
    }
    
    private void HandleTouchBegan(int fingerId, Vector2 screenPos)
    {
        // If camera control is already active, ignore all other touches
        if (cameraControlTouchId != -1)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"MobileCameraController: Ignoring touch {fingerId} - camera control already active with touch {cameraControlTouchId}");
            }
            return;
        }
        
        // Check if touch is over UI element
        if (IsTouchOverUI(screenPos))
        {
            if (enableDebugLogging)
            {
                Debug.Log($"MobileCameraController: Touch {fingerId} over UI - ignoring");
            }
            return;
        }
        
        // Check if touch is in exclusion area
        if (IsTouchInExclusionArea(screenPos))
        {
            if (enableDebugLogging)
            {
                Debug.Log($"MobileCameraController: Touch {fingerId} in exclusion area - ignoring");
            }
            return;
        }
        
        // Check if touch is in camera control area (if specified)
        if (cameraControlArea != null && !IsTouchInArea(screenPos, cameraControlArea))
        {
            if (enableDebugLogging)
            {
                Debug.Log($"MobileCameraController: Touch {fingerId} outside camera control area - ignoring");
            }
            return;
        }
        
        // Use this touch for camera control
        cameraControlTouchId = fingerId;
        activeTouches[fingerId] = screenPos;
        
        if (enableDebugLogging)
        {
            Debug.Log($"MobileCameraController: Started camera control with touch {fingerId} at {screenPos}");
        }
    }
    
    private void HandleTouchMoved(int fingerId, Vector2 screenPos, Vector2 deltaPosition)
    {
        // Only process camera movement for the designated camera control touch
        if (fingerId != cameraControlTouchId)
        {
            return;
        }
        
        // Update touch position
        activeTouches[fingerId] = screenPos;
        
        // Apply camera rotation
        ApplyCameraRotation(deltaPosition);
    }
    
    private void HandleTouchEnded(int fingerId)
    {
        // Remove from active touches
        activeTouches.Remove(fingerId);
        
        // If this was the camera control touch, clear it
        if (fingerId == cameraControlTouchId)
        {
            cameraControlTouchId = -1;
            
            if (enableDebugLogging)
            {
                Debug.Log($"MobileCameraController: Ended camera control with touch {fingerId}");
            }
        }
    }
    
    private void CleanupEndedTouches()
    {
        // Remove touches that are no longer active
        List<int> touchesToRemove = new List<int>();
        
        foreach (var kvp in activeTouches)
        {
            bool touchStillActive = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == kvp.Key)
                {
                    touchStillActive = true;
                    break;
                }
            }
            
            if (!touchStillActive)
            {
                touchesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int touchId in touchesToRemove)
        {
            if (touchId == cameraControlTouchId)
            {
                cameraControlTouchId = -1;
            }
            activeTouches.Remove(touchId);
        }
    }
    
    private void ApplyCameraRotation(Vector2 deltaPosition)
    {
        if (fpsController == null) return;
        
        // Get touch sensitivity from SettingsManager (device-specific)
        float touchSensitivity = 2f; // Default fallback
        if (SettingsManager.Instance != null)
        {
            touchSensitivity = SettingsManager.Instance.CurrentSettings.touchSensitivity;
        }
        
        // Convert delta to rotation using touch sensitivity
        // Don't use Time.deltaTime for touch input - deltaPosition is already frame-based
        float sensitivity = touchSensitivity * 0.01f; // Scale factor for touch input
        float deltaX = deltaPosition.x * sensitivity;
        float deltaY = deltaPosition.y * sensitivity;
        
        if (invertY)
        {
            deltaY = -deltaY;
        }
        
        // Apply rotation through FPS controller's mouse look system
        ApplyExternalMouseInput(deltaX, -deltaY); // Negative Y for proper camera control
    }
    
    private void ApplyExternalMouseInput(float mouseX, float mouseY)
    {
        if (fpsController != null)
        {
            // Send external mouse input to FirstPersonController
            fpsController.SetExternalMouseInput(mouseX, mouseY);
        }
    }
    
    private bool IsTouchOverUI(Vector2 screenPos)
    {
        // Check if touch is over any UI element using EventSystem
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        return results.Count > 0;
    }
    
    private bool IsTouchInExclusionArea(Vector2 screenPos)
    {
        if (exclusionAreas == null) return false;
        
        foreach (RectTransform exclusionArea in exclusionAreas)
        {
            if (exclusionArea != null && IsTouchInArea(screenPos, exclusionArea))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsTouchInArea(Vector2 screenPos, RectTransform area)
    {
        if (area == null) return false;
        
        // Convert screen position to local position relative to the RectTransform
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, screenPos, null, out localPos);
        
        // Check if the local position is within the rect bounds
        Rect rect = area.rect;
        return rect.Contains(localPos);
    }
    
    /// <summary>
    /// Add an exclusion area where touches should not control camera
    /// </summary>
    public void AddExclusionArea(RectTransform area)
    {
        if (area == null) return;
        
        if (exclusionAreas == null)
        {
            exclusionAreas = new RectTransform[] { area };
        }
        else
        {
            RectTransform[] newArray = new RectTransform[exclusionAreas.Length + 1];
            exclusionAreas.CopyTo(newArray, 0);
            newArray[exclusionAreas.Length] = area;
            exclusionAreas = newArray;
        }
    }
    
    /// <summary>
    /// Set the camera control area (if null, entire screen is used)
    /// </summary>
    public void SetCameraControlArea(RectTransform area)
    {
        cameraControlArea = area;
    }
    
    /// <summary>
    /// Get current touch sensitivity from SettingsManager
    /// </summary>
    public float TouchSensitivity
    {
        get 
        { 
            return SettingsManager.Instance != null ? 
                   SettingsManager.Instance.CurrentSettings.touchSensitivity : 2f; 
        }
        set 
        { 
            if (SettingsManager.Instance != null) 
            {
                SettingsManager.Instance.CurrentSettings.touchSensitivity = value;
            }
        }
    }
    
    /// <summary>
    /// Check if mobile camera control is currently enabled
    /// </summary>
    public bool IsEnabled
    {
        get { return isEnabled; }
    }
    
    /// <summary>
    /// Get the camera control area (if set)
    /// </summary>
    public RectTransform CameraControlArea
    {
        get { return cameraControlArea; }
    }
    
    /// <summary>
    /// Get the exclusion areas array
    /// </summary>
    public RectTransform[] ExclusionAreas
    {
        get { return exclusionAreas; }
    }
    
    /// <summary>
    /// Check if a screen position is in a valid camera control area
    /// </summary>
    public bool IsValidCameraArea(Vector2 screenPos)
    {
        // Check if touch is over UI element
        if (IsTouchOverUI(screenPos))
        {
            return false;
        }
        
        // Check if touch is in exclusion area
        if (IsTouchInExclusionArea(screenPos))
        {
            return false;
        }
        
        // Check if touch is in camera control area (if specified)
        if (cameraControlArea != null && !IsTouchInArea(screenPos, cameraControlArea))
        {
            return false;
        }
        
        return true;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Simulate mouse input as touch for editor testing
    /// </summary>
    private void SimulateMouseAsTouch()
    {
        const int MOUSE_FINGER_ID = 999; // Use high ID to avoid conflicts with real touches
        
        if (Input.GetMouseButtonDown(0))
        {
            // Mouse button pressed - simulate touch began
            HandleTouchBegan(MOUSE_FINGER_ID, Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            // Mouse button held - simulate touch moved
            Vector2 currentMousePos = Input.mousePosition;
            // Use raw mouse delta (similar to touch deltaPosition)
            Vector2 deltaPosition = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * Screen.width * 0.01f;
            HandleTouchMoved(MOUSE_FINGER_ID, currentMousePos, deltaPosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Mouse button released - simulate touch ended
            HandleTouchEnded(MOUSE_FINGER_ID);
        }
    }
    #endif
}
