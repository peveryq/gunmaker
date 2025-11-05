using UnityEngine;

public class Blowtorch : MonoBehaviour, IInteractable
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject flameEffect; // Sprite + particle system for flame
    [SerializeField] private Transform flameTip; // Point where flame appears
    [SerializeField] private Vector3 workingPositionOffset = new Vector3(0.1f, -0.05f, 0.05f); // Offset when working
    [SerializeField] private float movementSpeed = 5f; // Speed of movement to working position
    
    [Header("Audio")]
    [SerializeField] private AudioSource blowtorchAudio;
    [SerializeField] private AudioClip startSound; // Sound when torch ignites
    [SerializeField] private AudioClip workingSound; // Looping sound while working
    
    [Header("Settings")]
    [SerializeField] private float weldingSpeed = 10f; // Percent per second
    
    private bool isWorking = false;
    private bool isStartSoundPlaying = false;
    private Vector3 currentOffset = Vector3.zero; // Current offset from heldPosition
    private ItemPickup itemPickup;
    private float startSoundTimer = 0f;
    
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
        // Only apply offset when held
        if (itemPickup == null || !itemPickup.IsHeld)
        {
            StopWorking();
            return;
        }
        
        // Smoothly lerp the offset
        Vector3 targetOffset = isWorking ? workingPositionOffset : Vector3.zero;
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * movementSpeed);
        
        // Apply offset on top of ItemPickup's heldPosition
        // ItemPickup sets position in Update, we modify it in LateUpdate
        transform.localPosition = itemPickup.OriginalLocalPosition + currentOffset;
        
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
    
    public void StartWorking()
    {
        if (isWorking) return;
        
        isWorking = true;
        
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

