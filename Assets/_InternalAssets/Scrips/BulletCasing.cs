using UnityEngine;

public class BulletCasing : MonoBehaviour
{
    [Header("Casing Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float fadeTime = 3f;
    [SerializeField] private bool useFadeOut = true;
    [SerializeField] private bool useScaleFade = true; // Scale down instead of transparency
    
    [Header("Physics")]
    [SerializeField] private float ejectionForce = 3f;
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
        originalScale = transform.localScale;
        
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
        
        // Start fading before destruction
        if (elapsedTime >= lifetime - fadeTime && !isFading)
        {
            isFading = true;
        }
        
        // Fade out
        if (isFading)
        {
            float fadeProgress = (elapsedTime - (lifetime - fadeTime)) / fadeTime;
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            if (useScaleFade)
            {
                // Scale down to zero
                float scale = 1f - fadeProgress;
                transform.localScale = originalScale * scale;
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

