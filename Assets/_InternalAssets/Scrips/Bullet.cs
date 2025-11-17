using System.Collections;
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
    private Coroutine lifetimeCoroutine;
    private Coroutine returnCoroutine;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }
    
    /// <summary>
    /// Reset bullet state for reuse from pool. Called by BulletPool.
    /// </summary>
    public void Reset()
    {
        hasHit = false;
        
        // Stop all coroutines if running
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
        
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        
        // Reset Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset TrailRenderer if present (clear old trail history)
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }
        
        // Also check in children (in case trail is on child object)
        trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
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
        
        // Schedule return to pool or destroy after lifetime
        if (BulletPool.Instance != null)
        {
            lifetimeCoroutine = StartCoroutine(ReturnToPoolAfterLifetime(lifetime));
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;
        
        ContactPoint contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
        Vector3 hitPoint = contact.point;
        Vector3 hitNormal = contact.normal;

        if (collision.contacts.Length == 0 && collision.collider != null)
        {
            hitPoint = collision.collider.ClosestPoint(transform.position);
            Vector3 direction = rb != null && rb.linearVelocity != Vector3.zero
                ? -rb.linearVelocity.normalized
                : (transform.position - hitPoint).normalized;
            hitNormal = direction;
        }

        bool handledByTarget = false;
        if (collision.collider != null)
        {
            handledByTarget = TryHandleTargetHit(hitPoint, hitNormal, collision.collider);
        }

        // Always spawn bullet hole for visual feedback, even if hit was handled by target
        if (collision.contacts.Length > 0)
        {
            // Use BulletHoleManager if available
            if (BulletHoleManager.Instance != null)
            {
                BulletHoleManager.Instance.SpawnBulletHole(hitPoint, hitNormal, collision.transform);
            }
            else if (hitEffect != null)
            {
                // Fallback to old system
                Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            }
        }
        
        // Apply damage if collision has health
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // Disable physics immediately to prevent bouncing
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Return to pool or destroy bullet (with small delay to ensure bullet hole is created)
        returnCoroutine = StartCoroutine(ReturnToPoolDelayed());
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = rb != null && rb.linearVelocity != Vector3.zero
            ? -rb.linearVelocity.normalized
            : -transform.forward;

        bool handledByTarget = TryHandleTargetHit(hitPoint, hitNormal, other);

        // Apply damage if collision has health
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // Disable physics immediately to prevent bouncing
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Return to pool or destroy bullet (with small delay to ensure effects are processed)
        StartCoroutine(ReturnToPoolDelayed());
    }

    private bool TryHandleTargetHit(Vector3 point, Vector3 normal, Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        ShootingTargetZone zone = collider.GetComponent<ShootingTargetZone>();
        if (zone == null)
        {
            zone = collider.GetComponentInParent<ShootingTargetZone>();
        }

        if (zone == null)
        {
            return false;
        }

        Vector3 resolvedNormal = normal;
        if (resolvedNormal == Vector3.zero)
        {
            if (rb != null && rb.linearVelocity != Vector3.zero)
            {
                resolvedNormal = -rb.linearVelocity.normalized;
            }
            else
            {
                resolvedNormal = -transform.forward;
            }
        }

        zone.ReportHit(point, resolvedNormal, damage);
        return true;
    }
    
    private IEnumerator ReturnToPoolAfterLifetime(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (BulletPool.Instance != null && gameObject.activeSelf)
        {
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        lifetimeCoroutine = null;
    }
    
    private IEnumerator ReturnToPoolDelayed()
    {
        // Small delay to ensure bullet hole and effects are processed
        yield return new WaitForEndOfFrame();
        
        if (BulletPool.Instance != null && gameObject.activeSelf)
        {
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        returnCoroutine = null;
    }
}

// Interface for damageable objects
public interface IDamageable
{
    void TakeDamage(float damage);
}
