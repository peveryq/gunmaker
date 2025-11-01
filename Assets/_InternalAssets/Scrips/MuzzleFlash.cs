using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float lifetime = 0.05f; // Very short flash
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private bool randomScale = true;
    [SerializeField] private float scaleVariation = 0.2f; // 0.8 to 1.2 scale
    
    [Header("Light Settings")]
    [SerializeField] private Light flashLight;
    [SerializeField] private float lightFadeSpeed = 20f;
    
    private float spawnTime;
    private float initialLightIntensity;
    private Vector3 originalScale;
    
    private void Start()
    {
        spawnTime = Time.time;
        originalScale = transform.localScale;
        
        // Random rotation around Z axis for variety
        if (randomRotation)
        {
            transform.Rotate(0, 0, Random.Range(0f, 360f));
        }
        
        // Random scale for variety
        if (randomScale)
        {
            float randomScaleFactor = Random.Range(1f - scaleVariation, 1f + scaleVariation);
            transform.localScale = originalScale * randomScaleFactor;
        }
        
        // Store initial light intensity
        if (flashLight != null)
        {
            initialLightIntensity = flashLight.intensity;
        }
        
        // Auto destroy
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        // Fade out light intensity over time
        if (flashLight != null)
        {
            float elapsedTime = Time.time - spawnTime;
            float fadeProgress = elapsedTime / lifetime;
            flashLight.intensity = Mathf.Lerp(initialLightIntensity, 0f, fadeProgress * lightFadeSpeed);
        }
    }
}

