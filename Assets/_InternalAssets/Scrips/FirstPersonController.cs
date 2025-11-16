using UnityEngine;
using DG.Tweening;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float gravity = 20f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minLookAngle = -90f;
    [SerializeField] private float maxLookAngle = 90f;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float cameraHeight = 1.7f;
    
    [Header("Head Bob Settings")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Footsteps")]
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource footstepAudioSource; // Fallback only
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float walkingStepInterval = 0.5f;
    [SerializeField] private float runningStepInterval = 0.3f;
    
    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;
    private bool isRunning;
    
    private float xRotation = 0f;
    private float yRotation = 0f;
    
    private float bobTimer = 0f;
    private Vector3 originalCameraPosition;
    private Vector3 headBobOffset;
    
    private float stepTimer = 0f;
    private bool isMoving = false;
    
    private bool cursorLocked = true;
    
    // FOV Kick (camera FOV expansion on shot)
    private float baseFOV; // Base FOV (stored at start, before any modifications)
    private float fovKickOffset = 0f; // Current FOV kick offset (added to base FOV)
    private float previousFOVKickOffset = 0f; // Previous frame's offset (to track changes)
    private Tween fovKickReturnTween;
    
    private void Start()
    {
        // Get or add CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // Setup camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                playerCamera = cameraObj.AddComponent<Camera>();
            }
        }
        
        // Setup camera position
        playerCamera.transform.SetParent(transform);
        originalCameraPosition = new Vector3(0, cameraHeight, 0);
        playerCamera.transform.localPosition = originalCameraPosition;
        
        // Store base FOV for FOV Kick calculations
        baseFOV = playerCamera.fieldOfView;
        
        // Setup ground check
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheck = groundCheckObj.transform;
            groundCheck.localPosition = Vector3.down;
        }
        
        // Setup audio source for footsteps
        // AudioSource is now optional (fallback only)
        // AudioManager will be used if available
        
        // Lock cursor
        if (lockCursorOnStart)
        {
            LockCursor();
        }
        
        // Set ground mask to default if not set
        if (groundMask.value == 0)
        {
            groundMask = 1; // Default layer
        }
    }
    
    private void Update()
    {
        HandleCursorToggle();
        
        // Check if grounded (always, even when cursor is unlocked)
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        // Only handle movement and camera when cursor is locked
        if (cursorLocked)
        {
            HandleMouseLook();
            HandleMovement();
            HandleHeadBob();
            HandleFootsteps();
        }
        
        // Apply gravity always (after jump to allow jump velocity to be set)
        ApplyGravity();
    }
    
    private void LateUpdate()
    {
        // Apply FOV Kick offset in LateUpdate to ensure it's applied after all other FOV changes
        // (e.g., aiming FOV changes from WeaponController)
        if (playerCamera != null)
        {
            float currentFOV = playerCamera.fieldOfView;
            
            // If offset changed or is non-zero, apply it
            // We subtract the previous offset (if any) to get the base FOV,
            // then add the current offset
            if (fovKickOffset != previousFOVKickOffset || fovKickOffset != 0f)
            {
                // Remove previous offset to get base FOV, then add current offset
                float baseFOVWithoutKick = currentFOV - previousFOVKickOffset;
                playerCamera.fieldOfView = baseFOVWithoutKick + fovKickOffset;
                previousFOVKickOffset = fovKickOffset;
            }
        }
    }
    
    private void HandleCursorToggle()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            if (cursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }
    }
    
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }
    
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }
    
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player horizontally
        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        
        // Rotate camera vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
    
    private void HandleMovement()
    {
        // Get input - use GetAxisRaw for instant response
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Determine if running
        isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // Calculate movement direction relative to player rotation
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.Normalize();
        
        // Determine current speed
        if (moveDirection.magnitude > 0.1f)
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
            isMoving = true;
        }
        else
        {
            currentSpeed = 0f;
            isMoving = false;
        }
        
        // Move character
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }
    
    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleHeadBob()
    {
        if (!enableHeadBob || !isGrounded)
        {
            headBobOffset = Vector3.zero;
            return;
        }
        
        // Calculate head bob
        if (isMoving)
        {
            float bobSpeed = isRunning ? bobFrequency * 1.5f : bobFrequency;
            bobTimer += bobSpeed * Time.deltaTime;
            
            float bobValue = bobCurve.Evaluate(Mathf.PingPong(bobTimer, 2f) / 2f);
            float horizontalBob = Mathf.Sin(bobTimer) * bobAmount * bobValue;
            float verticalBob = bobValue * bobAmount;
            
            headBobOffset = new Vector3(horizontalBob, verticalBob, 0);
        }
        else
        {
            bobTimer = 0f;
            headBobOffset = Vector3.zero;
        }
        
        // Apply head bob to camera
        playerCamera.transform.localPosition = originalCameraPosition + headBobOffset;
    }
    
    private void HandleFootsteps()
    {
        if (!isGrounded || !isMoving || footstepSounds.Length == 0)
            return;
        
        float stepInterval = isRunning ? runningStepInterval : walkingStepInterval;
        stepTimer += Time.deltaTime;
        
        if (stepTimer >= stepInterval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }
    
    private void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;
        
        // Use same sounds for walking and running
        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        if (clip == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume: 0.5f);
        }
        else if (footstepAudioSource != null)
        {
            footstepAudioSource.PlayOneShot(clip);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
    
    // Public property to access camera
    public Camera PlayerCamera => playerCamera;
    
    // Public properties for weapon sway
    public bool IsMoving => isMoving;
    public bool IsRunning => isRunning;
    public float CurrentSpeed => currentSpeed;
    
    // Public method to apply camera recoil from weapons
    public void ApplyCameraRecoil(float verticalRecoil, float horizontalRecoil = 0f)
    {
        xRotation -= verticalRecoil; // Negative because positive xRotation looks down
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);
        yRotation += horizontalRecoil;
    }
    
    /// <summary>
    /// Apply FOV Kick - instant camera FOV expansion on shot with smooth return.
    /// Creates the powerful feel like in CoD (camera widens on shot).
    /// </summary>
    /// <param name="fovKickAmount">FOV expansion amount in degrees (positive = wider)</param>
    /// <param name="kickDuration">Duration of the FOV expansion (very short, 0.01-0.05s)</param>
    /// <param name="returnDuration">Duration of smooth FOV return to original (0.1-0.2s)</param>
    public void ApplyFOVKick(float fovKickAmount, float kickDuration = 0.03f, float returnDuration = 0.15f)
    {
        if (playerCamera == null) return;
        
        // Kill existing return tween if any
        if (fovKickReturnTween != null && fovKickReturnTween.IsActive())
        {
            fovKickReturnTween.Kill();
        }
        
        // Apply instant FOV expansion
        fovKickOffset = fovKickAmount;
        
        // Then smoothly return to zero
        fovKickReturnTween = DOTween.To(
            () => fovKickOffset,
            x => fovKickOffset = x,
            0f,
            returnDuration
        )
        .SetEase(Ease.OutQuad) // Smooth deceleration
        .OnComplete(() => {
            fovKickOffset = 0f; // Ensure it's exactly zero
        });
    }
    
    private void OnDestroy()
    {
        // Clean up DOTween tweens
        if (fovKickReturnTween != null && fovKickReturnTween.IsActive())
        {
            fovKickReturnTween.Kill();
        }
    }
}
