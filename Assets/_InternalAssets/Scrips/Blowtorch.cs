using UnityEngine;

public class Blowtorch : MonoBehaviour, IInteractable
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject flameEffect; // Sprite + particle system for flame
    [SerializeField] private Transform flameTip; // Point where flame appears
    [SerializeField] private float movementSpeed = 5f; // Speed of movement to working position
    [SerializeField] private float rotationSpeed = 10f; // Speed of rotation to working position
    
    [Header("Audio")]
    [SerializeField] private AudioSource blowtorchAudio;
    [SerializeField] private AudioClip startSound; // Sound when torch ignites
    [SerializeField] private AudioClip workingSound; // Looping sound while working
    
    [Header("Settings")]
    [SerializeField] private float weldingSpeed = 10f; // Percent per second
    
    [Header("Controls")]
    [SerializeField] private KeyCode weldKey = KeyCode.E;
    
    private bool isWorking = false;
    private bool isStartSoundPlaying = false;
    private ItemPickup itemPickup;
    private float startSoundTimer = 0f;
    
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
        
        // Handle start sound to working sound transition
        if (isStartSoundPlaying && blowtorchAudio != null)
        {
            startSoundTimer += Time.deltaTime;
            
            // Check if start sound finished playing
            if (startSound != null && startSoundTimer >= startSound.length)
            {
                // Transition to working sound
                TransitionToWorkingSound();
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
        
        // Play start sound first
        if (blowtorchAudio != null && startSound != null)
        {
            blowtorchAudio.loop = false;
            blowtorchAudio.clip = startSound;
            blowtorchAudio.Play();
            isStartSoundPlaying = true;
            startSoundTimer = 0f;
        }
        else
        {
            // If no start sound, play working sound immediately
            TransitionToWorkingSound();
        }
    }
    
    private void TransitionToWorkingSound()
    {
        if (blowtorchAudio != null && workingSound != null && isWorking)
        {
            blowtorchAudio.clip = workingSound;
            blowtorchAudio.loop = true;
            blowtorchAudio.Play();
            isStartSoundPlaying = false;
        }
    }
    
    public void StopWorking()
    {
        if (!isWorking) return;
        
        isWorking = false;
        isStartSoundPlaying = false;
        startSoundTimer = 0f;
        isMovingToWorkPosition = true; // Start returning to held position
        
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }
        
        if (blowtorchAudio != null)
        {
            blowtorchAudio.Stop();
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

