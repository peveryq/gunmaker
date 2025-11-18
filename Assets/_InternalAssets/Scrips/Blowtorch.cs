using UnityEngine;

public class Blowtorch : MonoBehaviour, IInteractable
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject flameEffect; // Sprite + particle system for flame
    [SerializeField] private Transform flameTip; // Point where flame appears
    [SerializeField] private float movementSpeed = 5f; // Speed of movement to working position
    [SerializeField] private float rotationSpeed = 10f; // Speed of rotation to working position
    
    [Header("Audio")]
    [Tooltip("Local AudioSource for looping working sound. AudioManager doesn't support looping SFX, so we use local AudioSource but apply AudioManager volume settings.")]
    [SerializeField] private AudioSource blowtorchAudio; // AudioSource for working sound (fallback if AudioManager unavailable)
    [SerializeField] private AudioClip workingSound; // Looping sound while working
    [SerializeField] private float workingSoundVolume = 0.8f; // Base volume for working sound (will be multiplied by AudioManager volume)
    
    [Header("Settings")]
    [SerializeField] private float weldingSpeed = 10f; // Percent per second
    
    [Header("Controls")]
    [SerializeField] private KeyCode weldKey = KeyCode.E;
    
    private bool isWorking = false;
    private ItemPickup itemPickup;
    
    // Working position control
    private Transform targetWorkingTransform; // Target position when working
    private Vector3 originalHeldPosition;
    private Quaternion originalHeldRotation;
    private bool isMovingToWorkPosition = false;
    
    private void Awake()
    {
        itemPickup = GetComponent<ItemPickup>();
        
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }
    }
    
    private void LateUpdate()
    {
        // Only work when held
        if (itemPickup == null || !itemPickup.IsHeld)
        {
            StopWorking();
            return;
        }
        
        // Handle position movement
        if (isWorking && targetWorkingTransform != null)
        {
            // Move to working position (world space)
            transform.position = Vector3.Lerp(transform.position, targetWorkingTransform.position, Time.deltaTime * movementSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetWorkingTransform.rotation, Time.deltaTime * rotationSpeed);
        }
        else if (!isWorking && isMovingToWorkPosition)
        {
            // Return to held position (local space)
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalHeldPosition, Time.deltaTime * movementSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalHeldRotation, Time.deltaTime * rotationSpeed);
            
            // Check if close enough to held position
            if (Vector3.Distance(transform.localPosition, originalHeldPosition) < 0.01f)
            {
                transform.localPosition = originalHeldPosition;
                transform.localRotation = originalHeldRotation;
                isMovingToWorkPosition = false;
            }
        }
        
        // Update working sound volume if playing (to reflect AudioManager volume changes)
        if (isWorking && blowtorchAudio != null && blowtorchAudio.isPlaying && blowtorchAudio.clip == workingSound)
        {
            if (AudioManager.Instance != null)
            {
                float masterVolume = AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume;
                blowtorchAudio.volume = workingSoundVolume * masterVolume;
            }
        }
    }
    
    public void StartWorking(Transform workPosition = null)
    {
        if (isWorking) return;
        
        isWorking = true;
        targetWorkingTransform = workPosition;
        
        // Store original held position
        if (itemPickup != null)
        {
            originalHeldPosition = itemPickup.OriginalLocalPosition;
            originalHeldRotation = Quaternion.Euler(Vector3.zero); // ItemPickup rotation
        }
        
        if (flameEffect != null)
        {
            flameEffect.SetActive(true);
        }
        
        // Start working sound immediately
        StartWorkingSound();
    }
    
    /// <summary>
    /// Start working sound.
    /// Uses local AudioSource (AudioManager doesn't support looping sounds),
    /// but applies AudioManager volume settings for consistency.
    /// </summary>
    private void StartWorkingSound()
    {
        if (blowtorchAudio == null || workingSound == null || !isWorking) return;
        
        // Prevent multiple calls
        if (blowtorchAudio.isPlaying && blowtorchAudio.clip == workingSound)
        {
            return;
        }
        
        // Stop any currently playing sound
        if (blowtorchAudio.isPlaying)
        {
            blowtorchAudio.Stop();
        }
        
        // Set and play working sound
        blowtorchAudio.clip = workingSound;
        blowtorchAudio.loop = true;
        
        // Apply AudioManager volume settings (priority) or fallback to base volume
        if (AudioManager.Instance != null)
        {
            // Use AudioManager volume settings
            blowtorchAudio.volume = workingSoundVolume * AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume;
        }
        else
        {
            // Fallback: use base volume if AudioManager unavailable
            blowtorchAudio.volume = workingSoundVolume;
        }
        
        blowtorchAudio.Play();
    }
    
    public void StopWorking()
    {
        if (!isWorking) return;
        
        isWorking = false;
        isMovingToWorkPosition = true; // Start returning to held position
        
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }
        
        // Stop audio source
        if (blowtorchAudio != null)
        {
            blowtorchAudio.Stop();
            blowtorchAudio.clip = null;
        }
    }
    
    public bool IsWorking => isWorking;
    public float WeldingSpeed => weldingSpeed;
    public KeyCode WeldKey => weldKey;
    
    // IInteractable implementation
    public bool Interact(InteractionHandler player)
    {
        return itemPickup?.Interact(player) ?? false;
    }
    
    public bool CanInteract(InteractionHandler player)
    {
        return itemPickup?.CanInteract(player) ?? false;
    }
    
    public string GetInteractionPrompt(InteractionHandler player)
    {
        return itemPickup?.GetInteractionPrompt(player) ?? "Pick up Blowtorch";
    }
    
    public Transform Transform => transform;
    public float InteractionRange => itemPickup?.InteractionRange ?? 2f;
    public bool ShowOutline => itemPickup?.ShowOutline ?? true;
}

