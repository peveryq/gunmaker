using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Universal button sound component that can be added to any Button GameObject.
/// Automatically plays sounds on click and hover events.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSoundComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Sounds")]
    [Tooltip("Sound to play when button is clicked")]
    [SerializeField] private AudioClip clickSound;
    [Tooltip("Sound to play when mouse enters button area (optional)")]
    [SerializeField] private AudioClip hoverSound;
    [Tooltip("Sound to play when trying to click disabled button (optional)")]
    [SerializeField] private AudioClip disabledSound;
    
    [Header("Sound Settings")]
    [Range(0f, 1f)]
    [Tooltip("Volume multiplier for all button sounds")]
    [SerializeField] private float volume = 1f;
    [Tooltip("Enable hover sound on mouse enter")]
    [SerializeField] private bool playOnHover = true;
    [Tooltip("Enable click sound on button press")]
    [SerializeField] private bool playOnClick = true;
    [Tooltip("Play sound even when button is disabled")]
    [SerializeField] private bool playDisabledSound = false;
    
    [Header("Audio Source (Optional)")]
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource audioSource; // Fallback only
    
    private Button button;
    private bool isHovering = false;
    
    private void Awake()
    {
        // Get Button component
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"ButtonSoundComponent: No Button component found on {gameObject.name}!");
            enabled = false;
            return;
        }
        
        // Subscribe to button click event
        button.onClick.AddListener(OnButtonClicked);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from button events
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// Called when button is clicked
    /// </summary>
    private void OnButtonClicked()
    {
        if (!playOnClick) return;
        
        // Check if button is interactable
        if (button.interactable)
        {
            PlaySound(clickSound);
        }
        else if (playDisabledSound)
        {
            PlaySound(disabledSound);
        }
    }
    
    /// <summary>
    /// Called when mouse enters button area
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playOnHover || isHovering) return;
        
        isHovering = true;
        
        // Only play hover sound if button is interactable
        if (button.interactable)
        {
            PlaySound(hoverSound);
        }
    }
    
    /// <summary>
    /// Called when mouse exits button area
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
    
    /// <summary>
    /// Play a sound using AudioManager or fallback AudioSource
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        // Try to use AudioManager first
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume);
        }
        // Fallback to local AudioSource
        else if (audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.PlayOneShot(clip);
        }
        else
        {
            // Last resort: create temporary AudioSource
            GameObject tempAudioObj = new GameObject("TempButtonAudio");
            AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();
            tempAudioSource.clip = clip;
            tempAudioSource.volume = volume;
            tempAudioSource.spatialBlend = 0f; // 2D sound
            tempAudioSource.Play();
            
            // Destroy temporary object after sound finishes
            Destroy(tempAudioObj, clip.length + 0.1f);
        }
    }
    
    /// <summary>
    /// Manually play click sound (for external calls)
    /// </summary>
    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }
    
    /// <summary>
    /// Manually play hover sound (for external calls)
    /// </summary>
    public void PlayHoverSound()
    {
        PlaySound(hoverSound);
    }
    
    /// <summary>
    /// Manually play disabled sound (for external calls)
    /// </summary>
    public void PlayDisabledSound()
    {
        PlaySound(disabledSound);
    }
    
    /// <summary>
    /// Set click sound at runtime
    /// </summary>
    public void SetClickSound(AudioClip clip)
    {
        clickSound = clip;
    }
    
    /// <summary>
    /// Set hover sound at runtime
    /// </summary>
    public void SetHoverSound(AudioClip clip)
    {
        hoverSound = clip;
    }
    
    /// <summary>
    /// Set disabled sound at runtime
    /// </summary>
    public void SetDisabledSound(AudioClip clip)
    {
        disabledSound = clip;
    }
    
    /// <summary>
    /// Set volume at runtime
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }
    
    /// <summary>
    /// Enable/disable hover sounds at runtime
    /// </summary>
    public void SetHoverEnabled(bool enabled)
    {
        playOnHover = enabled;
    }
    
    /// <summary>
    /// Enable/disable click sounds at runtime
    /// </summary>
    public void SetClickEnabled(bool enabled)
    {
        playOnClick = enabled;
    }
}
