using UnityEngine;

/// <summary>
/// Simplified centralized audio management system with volume controls.
/// Uses two AudioSource objects: one for SFX, one for Music.
/// 
/// Features:
/// - Separate volume controls for SFX, Music, and Master
/// - Global audio settings persistence
/// - Simple 2D sound playback (no spatialization)
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource for sound effects (SFX). If not assigned, will be created automatically.")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("AudioSource for background music. If not assigned, will be created automatically.")]
    [SerializeField] private AudioSource musicSource;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;
    
    [Header("Performance Settings")]
    [Tooltip("Maximum number of simultaneous SFX sounds. Prevents audio spam. 0 = unlimited.")]
    [SerializeField] private int maxSimultaneousSFX = 10;
    [Tooltip("Time window (in seconds) to track simultaneous sounds.")]
    [SerializeField] private float simultaneousSoundWindow = 0.1f;
    
    private static AudioManager instance;
    public static AudioManager Instance => instance;
    
    private System.Collections.Generic.Queue<float> recentSFXTimes = new System.Collections.Generic.Queue<float>();
    
    // Volume properties with persistence
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
            UpdateAllVolumes();
        }
    }
    
    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("Audio_SFXVolume", sfxVolume);
            UpdateAllVolumes();
        }
    }
    
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("Audio_MusicVolume", musicVolume);
            UpdateMusicVolume();
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            
            // Move to root if parented (DontDestroyOnLoad only works for root objects)
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            InitializeSources();
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSources()
    {
        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f; // 2D sound
            sfxSource.playOnAwake = false;
        }
        else
        {
            // Ensure SFX source is configured for 2D
            sfxSource.spatialBlend = 0f;
        }
        
        // Create music source if not assigned
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // 2D sound
        }
        else
        {
            // Ensure music source is configured
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }
    }
    
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("Audio_MusicVolume", 1f);
        UpdateAllVolumes();
    }
    
    /// <summary>
    /// Play a sound effect (2D, no spatialization).
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;
        
        // Check simultaneous sound limit
        if (maxSimultaneousSFX > 0)
        {
            float currentTime = Time.time;
            
            // Remove old entries outside the time window
            while (recentSFXTimes.Count > 0 && currentTime - recentSFXTimes.Peek() > simultaneousSoundWindow)
            {
                recentSFXTimes.Dequeue();
            }
            
            // Check if we've exceeded the limit
            if (recentSFXTimes.Count >= maxSimultaneousSFX)
            {
                // Skip this sound to prevent spam
                return;
            }
            
            // Add current time to tracking
            recentSFXTimes.Enqueue(currentTime);
        }
        
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }
    
    /// <summary>
    /// Play a sound effect (2D, no spatialization). Alias for PlaySFX for consistency.
    /// </summary>
    public void PlaySFX2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        PlaySFX(clip, volume, pitch);
    }
    
    /// <summary>
    /// Play a one-shot sound effect (simpler API).
    /// </summary>
    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        PlaySFX(clip, volume);
    }
    
    /// <summary>
    /// Play background music (looping).
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (musicSource == null) return;
        
        musicSource.clip = clip;
        musicSource.volume = volume * musicVolume * masterVolume;
        musicSource.loop = loop;
        musicSource.Play();
    }
    
    /// <summary>
    /// Stop background music.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    /// <summary>
    /// Pause background music.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }
    
    /// <summary>
    /// Resume background music.
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }
    
    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        // SFX volume is applied per-call in PlaySFX, so no need to update here
    }
    
    private void UpdateMusicVolume()
    {
        if (musicSource != null && musicSource.clip != null)
        {
            // Preserve original volume multiplier
            float originalVolume = musicSource.volume / (musicVolume * masterVolume);
            musicSource.volume = originalVolume * musicVolume * masterVolume;
        }
    }
    
    /// <summary>
    /// Stop all currently playing sounds (useful for pause menu, etc.).
    /// Note: This only stops music, as SFX are one-shot and can't be stopped individually.
    /// </summary>
    public void StopAllSFX()
    {
        // SFX are one-shot, so we can't stop them individually
        // This method is kept for API compatibility
        StopMusic();
    }
}
