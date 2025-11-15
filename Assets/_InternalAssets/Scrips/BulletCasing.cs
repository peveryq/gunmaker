using UnityEngine;

public class BulletCasing : MonoBehaviour
{
    [Header("Casing Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float fadeTime = 3f;
    [SerializeField] private bool useFadeOut = true;
    [SerializeField] private bool useScaleFade = true; // Scale down instead of transparency
    
    [Header("Physics")]
    [SerializeField] private float randomTorque = 5f;
    
    private Rigidbody rb;
    private Renderer[] renderers;
    private Material[] materialInstances;
    private float spawnTime;
    private bool isFading = false;
    private bool hasEjected = false;
    private Vector3 originalScale;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>();
        spawnTime = Time.time;
        
        // Ensure fadeTime is valid
        if (fadeTime <= 0f)
        {
            fadeTime = 0.1f; // Default minimum value
        }
        
        // Ensure lifetime is valid
        if (lifetime <= 0f)
        {
            lifetime = 1f; // Default minimum value
        }
        
        // Ensure fadeTime doesn't exceed lifetime
        if (fadeTime > lifetime)
        {
            fadeTime = lifetime * 0.5f; // Use half of lifetime as fade time
        }
        
        // Get original scale and validate it
        originalScale = transform.localScale;
        if (float.IsNaN(originalScale.x) || float.IsNaN(originalScale.y) || float.IsNaN(originalScale.z) ||
            float.IsInfinity(originalScale.x) || float.IsInfinity(originalScale.y) || float.IsInfinity(originalScale.z) ||
            originalScale.x <= 0f || originalScale.y <= 0f || originalScale.z <= 0f)
        {
            // Fallback to Vector3.one if scale is invalid
            originalScale = Vector3.one;
            transform.localScale = Vector3.one;
        }
        
        // Create material instances for fade effect
        if (useFadeOut && !useScaleFade)
        {
            SetupTransparentMaterials();
        }
    }
    
    private void SetupTransparentMaterials()
    {
        // Create material instances to avoid affecting other objects
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material[] mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = new Material(mats[i]);
                    
                    // Set material to Fade mode for transparency
                    mat.SetFloat("_Mode", 2); // Fade
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    
                    mats[i] = mat;
                }
                renderer.materials = mats;
            }
        }
    }
    
    public void Eject(Vector3 direction, float force)
    {
        if (hasEjected) return;
        hasEjected = true;
        
        // Get rigidbody if not yet initialized
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        if (rb != null)
        {
            // Apply ejection force
            rb.AddForce(direction * force, ForceMode.Impulse);
            
            // Add random torque for realistic spinning
            Vector3 randomTorqueVector = new Vector3(
                Random.Range(-randomTorque, randomTorque),
                Random.Range(-randomTorque, randomTorque),
                Random.Range(-randomTorque, randomTorque)
            );
            rb.AddTorque(randomTorqueVector, ForceMode.Impulse);
        }
    }
    
    private void Update()
    {
        if (!useFadeOut)
        {
            // Simple destruction without fade
            if (Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
            return;
        }
        
        float elapsedTime = Time.time - spawnTime;
        
        // Ensure fadeTime is valid for calculations
        float safeFadeTime = fadeTime > 0.0001f ? fadeTime : 0.1f;
        if (safeFadeTime > lifetime)
        {
            safeFadeTime = lifetime * 0.5f;
        }
        
        // Start fading before destruction
        if (elapsedTime >= lifetime - safeFadeTime && !isFading)
        {
            isFading = true;
        }
        
        // Fade out
        if (isFading)
        {
            // Use the safeFadeTime calculated above
            float fadeProgress = (elapsedTime - (lifetime - safeFadeTime)) / safeFadeTime;
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            if (useScaleFade)
            {
                // Scale down to zero
                float scale = 1f - fadeProgress;
                
                // Validate scale value before applying
                if (!float.IsNaN(scale) && !float.IsInfinity(scale) && scale >= 0f)
                {
                    Vector3 newScale = originalScale * scale;
                    
                    // Validate newScale before applying
                    if (!float.IsNaN(newScale.x) && !float.IsNaN(newScale.y) && !float.IsNaN(newScale.z) &&
                        !float.IsInfinity(newScale.x) && !float.IsInfinity(newScale.y) && !float.IsInfinity(newScale.z))
                    {
                        transform.localScale = newScale;
                    }
                }
            }
            else
            {
                // Transparency fade
                float alpha = 1f - fadeProgress;
                
                if (renderers != null)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            foreach (Material mat in renderer.materials)
                            {
                                Color color = mat.color;
                                color.a = alpha;
                                mat.color = color;
                            }
                        }
                    }
                }
            }
        }
        
        // Destroy after lifetime
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    // Optional: make casing settle and stop moving after landing
    private void OnCollisionEnter(Collision collision)
    {
        // Add sound effect for casing hitting ground
        // You can add AudioSource and play clink sound here
    }
}

