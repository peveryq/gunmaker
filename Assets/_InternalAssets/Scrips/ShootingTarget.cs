using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DamageNumbersPro;

/// <summary>
/// Configurable shooting target with payout, audio, animation, and particle handling.
/// </summary>
public class ShootingTarget : MonoBehaviour
{
    public enum HitZone
    {
        Normal,
        Bullseye
    }

    [Header("Reward Settings")]
    [SerializeField] private int baseReward = 10;
    [SerializeField] private float normalMultiplier = 1f;
    [SerializeField] private float bullseyeMultiplier = 1.5f;
    [SerializeField] private bool suppressRewardsWhileDown = true;
    [Tooltip("Additional reward given when target HP reaches 0 (kill reward).")]
    [SerializeField] private int killReward = 50;

    [Header("Health Settings")]
    [Tooltip("Maximum health points for this target.")]
    [SerializeField] private float maxHP = 100f;
    [Tooltip("Damage multiplier when hitting bullseye zone.")]
    [SerializeField] private float bullseyeDamageMultiplier = 1.5f;

    [Header("Audio Settings")]
    [Tooltip("Audio clips for hits. If AudioManager is available, uses it. Otherwise falls back to local AudioSource (if assigned).")]
    [SerializeField] private AudioClip normalHitClip;
    [SerializeField] private AudioClip bullseyeHitClip;
    [Tooltip("Optional additional variants for normal hits; chosen randomly.")]
    [SerializeField] private AudioClip[] normalHitVariants;
    [Tooltip("Optional additional variants for bullseye hits; chosen randomly.")]
    [SerializeField] private AudioClip[] bullseyeHitVariants;
    [Tooltip("Sound played when target starts falling animation.")]
    [SerializeField] private AudioClip fallSound;
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource audioSource; // Fallback only

    [Header("Falling Behaviour")]
    [SerializeField] private bool enableFalling = false;
    [Tooltip("DOTweenAnimation component for falling animation. Should animate target to fallen position (e.g., rotate 90 degrees). Set autoPlay = false in DOTweenAnimation.")]
    [SerializeField] private DOTweenAnimation fallAnimation;
    [Tooltip("DOTweenAnimation component for reset/raise animation. Should animate target back to upright position. Set autoPlay = false in DOTweenAnimation.")]
    [SerializeField] private DOTweenAnimation resetAnimation;
    [Tooltip("How long the target stays down before automatically raising.")]
    [SerializeField] private float timeDown = 2f;

    [Header("Damage Numbers")]
    [Tooltip("Prefab for spawning money numbers when target is hit. Uses Damage Numbers Pro.")]
    [SerializeField] private DamageNumber moneyNumberPrefab;
    [Tooltip("Offset from hit point where money number should spawn.")]
    [SerializeField] private Vector3 moneyNumberOffset = Vector3.up * 0.5f;

    private Coroutine resetCoroutine;
    private bool isDown;
    private float currentHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// Register a hit from a projectile and trigger feedback/reward logic.
    /// </summary>
    public void RegisterHit(HitZone zone, Vector3 hitPoint, Vector3 hitNormal, float damage = 0f)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        // Apply damage if provided
        if (damage > 0f)
        {
            // Apply bullseye damage multiplier if hitting bullseye
            float finalDamage = damage;
            if (zone == HitZone.Bullseye)
            {
                finalDamage = damage * Mathf.Max(0f, bullseyeDamageMultiplier);
            }
            
            currentHP = Mathf.Max(0f, currentHP - finalDamage);
        }

        bool rewardAllowed = !suppressRewardsWhileDown || !isDown;
        int reward = 0;
        if (rewardAllowed && MoneySystem.Instance != null)
        {
            reward = Mathf.Max(0, Mathf.RoundToInt(baseReward * GetMultiplier(zone)));
            if (reward > 0)
            {
                MoneySystem.Instance.AddMoney(reward);
                
                // Spawn money number at hit point
                SpawnMoneyNumber(hitPoint, reward);
            }
        }

        PlayHitSound(zone);

        // Show hit lines on crosshair for any hit
        if (GameplayHUD.Instance != null)
        {
            CrosshairController.HitZone crosshairZone = zone == HitZone.Bullseye 
                ? CrosshairController.HitZone.Bullseye 
                : CrosshairController.HitZone.Normal;
            GameplayHUD.Instance.ShowHitLines(crosshairZone);
        }

        // Check if target was killed (HP reached zero)
        bool wasKilled = currentHP <= 0f;
        
        // Give kill reward when target is killed (only once, when it wasn't down before)
        if (wasKilled && !isDown && killReward > 0 && MoneySystem.Instance != null)
        {
            MoneySystem.Instance.AddMoney(killReward);
            
            // Spawn kill reward money number
            SpawnMoneyNumber(hitPoint, killReward);
        }
        
        // Trigger falling if HP reaches zero or if falling is enabled and HP is zero
        if (enableFalling && wasKilled && !isDown)
        {
            // Show kill lines when target is killed
            if (GameplayHUD.Instance != null)
            {
                GameplayHUD.Instance.ShowKillLines();
            }
            
            HandleFalling();
        }
    }

    /// <summary>
    /// Determines if the target currently considers itself down.
    /// </summary>
    public bool IsDown => isDown;

    private float GetMultiplier(HitZone zone)
    {
        switch (zone)
        {
            case HitZone.Bullseye:
                return Mathf.Max(0f, bullseyeMultiplier);
            default:
                return Mathf.Max(0f, normalMultiplier);
        }
    }

    private void PlayHitSound(HitZone zone)
    {
        AudioClip clip = zone == HitZone.Bullseye
            ? GetRandomClip(bullseyeHitVariants, bullseyeHitClip)
            : GetRandomClip(normalHitVariants, normalHitClip);
        
        if (clip == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume: 0.8f);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayFallSound()
    {
        if (fallSound == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(fallSound, volume: 0.7f);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(fallSound);
        }
    }

    private void HandleFalling()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        bool wasDown = isDown;
        resetCoroutine = StartCoroutine(FallRoutine(wasDown));
    }

    private IEnumerator FallRoutine(bool skipFallAnimation)
    {
        isDown = true;

        if (!skipFallAnimation && fallAnimation != null)
        {
            // Ensure tween is created if autoGenerate is false
            // CreateTween with regenerateIfExists=false means it won't recreate if already exists
            fallAnimation.CreateTween(false, false);

            // Rewind reset animation first to ensure clean state
            if (resetAnimation != null)
            {
                resetAnimation.CreateTween(false, false);
                resetAnimation.DORewind();
            }

            // Play fall animation
            fallAnimation.DORestart();
            PlayFallSound();
        }

        // Wait for the target to stay down
        float wait = Mathf.Max(0f, timeDown);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }

        // Play reset animation to raise the target
        if (resetAnimation != null)
        {
            resetAnimation.CreateTween(false, false);
            resetAnimation.DORestart();
        }

        // Restore HP when target is raised
        currentHP = maxHP;
        isDown = false;
        resetCoroutine = null;
    }

    private static AudioClip GetRandomClip(IReadOnlyList<AudioClip> variants, AudioClip fallback)
    {
        if (variants != null && variants.Count > 0)
        {
            int index = Random.Range(0, variants.Count);
            AudioClip variant = variants[index];
            if (variant != null)
            {
                return variant;
            }
        }

        return fallback;
    }

    /// <summary>
    /// Spawns a money number popup at the specified position using Damage Numbers Pro.
    /// </summary>
    private void SpawnMoneyNumber(Vector3 position, int amount)
    {
        if (moneyNumberPrefab == null)
        {
            return;
        }

        // Calculate spawn position with offset
        Vector3 spawnPosition = position + moneyNumberOffset;
        
        // Spawn the damage number
        moneyNumberPrefab.Spawn(spawnPosition, amount);
    }

    private void OnDisable()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        isDown = false;
    }
}

