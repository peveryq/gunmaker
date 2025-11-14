using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalHitClip;
    [SerializeField] private AudioClip bullseyeHitClip;
    [Tooltip("Optional additional variants for normal hits; chosen randomly.")]
    [SerializeField] private AudioClip[] normalHitVariants;
    [Tooltip("Optional additional variants for bullseye hits; chosen randomly.")]
    [SerializeField] private AudioClip[] bullseyeHitVariants;
    [Tooltip("Sound played when target starts falling animation.")]
    [SerializeField] private AudioClip fallSound;

    [Header("Falling Behaviour")]
    [SerializeField] private bool enableFalling = false;
    [SerializeField] private Animator animator;
    [SerializeField] private string fallTriggerName = "Fall";
    [SerializeField] private string resetTriggerName = "Reset";
    [SerializeField] private float timeDown = 2f;

    [Header("Particle Overrides")]
    [Tooltip("Optional particle prefab for normal hits. Instantiated once and reused.")]
    [SerializeField] private ParticleSystem normalHitParticles;
    [Tooltip("Optional particle prefab for bullseye hits. Instantiated once and reused.")]
    [SerializeField] private ParticleSystem bullseyeHitParticles;
    [Tooltip("Optional parent transform for spawned particle instances.")]
    [SerializeField] private Transform particleParent;

    private readonly Dictionary<HitZone, ParticleSystem> particleInstances = new Dictionary<HitZone, ParticleSystem>();
    private Coroutine resetCoroutine;
    private bool isDown;

    /// <summary>
    /// Register a hit from a projectile and trigger feedback/reward logic.
    /// </summary>
    public void RegisterHit(HitZone zone, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        bool rewardAllowed = !suppressRewardsWhileDown || !isDown;
        if (rewardAllowed && MoneySystem.Instance != null)
        {
            int reward = Mathf.Max(0, Mathf.RoundToInt(baseReward * GetMultiplier(zone)));
            if (reward > 0)
            {
                MoneySystem.Instance.AddMoney(reward);
            }
        }

        PlayHitSound(zone);
        SpawnHitParticles(zone, hitPoint, hitNormal);

        if (enableFalling)
        {
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
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip = zone == HitZone.Bullseye
            ? GetRandomClip(bullseyeHitVariants, bullseyeHitClip)
            : GetRandomClip(normalHitVariants, normalHitClip);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayFallSound()
    {
        if (audioSource != null && fallSound != null)
        {
            audioSource.PlayOneShot(fallSound);
        }
    }

    private void SpawnHitParticles(HitZone zone, Vector3 hitPoint, Vector3 hitNormal)
    {
        ParticleSystem prefab = zone == HitZone.Bullseye ? bullseyeHitParticles : normalHitParticles;
        if (prefab == null)
        {
            return;
        }

        if (!particleInstances.TryGetValue(zone, out ParticleSystem instance) || instance == null)
        {
            instance = Instantiate(prefab, particleParent != null ? particleParent : transform);
            particleInstances[zone] = instance;
        }

        instance.transform.position = hitPoint;
        instance.transform.rotation = Quaternion.LookRotation(hitNormal);
        instance.gameObject.SetActive(true);
        instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        instance.Play();
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

    private IEnumerator FallRoutine(bool skipFallTrigger)
    {
        isDown = true;

        if (!skipFallTrigger && animator != null)
        {
            if (!string.IsNullOrEmpty(resetTriggerName))
            {
                animator.ResetTrigger(resetTriggerName);
            }

            if (!string.IsNullOrEmpty(fallTriggerName))
            {
                animator.SetTrigger(fallTriggerName);
                PlayFallSound();
                
                // Show kill lines on crosshair
                if (GameplayHUD.Instance != null)
                {
                    GameplayHUD.Instance.ShowKillLines();
                }
            }
        }

        float wait = Mathf.Max(0f, timeDown);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(fallTriggerName))
            {
                animator.ResetTrigger(fallTriggerName);
            }

            if (!string.IsNullOrEmpty(resetTriggerName))
            {
                animator.SetTrigger(resetTriggerName);
            }
        }

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

