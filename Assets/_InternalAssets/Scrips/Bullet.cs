using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private GameObject hitEffect;
    
    private Rigidbody rb;
    private bool hasHit = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }
    
    public void Initialize(Vector3 direction, float bulletSpeed, float bulletDamage)
    {
        speed = bulletSpeed;
        damage = bulletDamage;
        
        // Apply velocity
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            ContactPoint contact = collision.contacts[0];
            Instantiate(hitEffect, contact.point, Quaternion.LookRotation(contact.normal));
        }
        
        // Apply damage if collision has health
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // Destroy bullet
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        hasHit = true;
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Apply damage if collision has health
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // Destroy bullet
        Destroy(gameObject);
    }
}

// Interface for damageable objects
public interface IDamageable
{
    void TakeDamage(float damage);
}
