using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for bullets to reduce Instantiate/Destroy calls.
/// WebGL-optimized: reuses bullet GameObjects instead of creating/destroying them.
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private int maxPoolSize = 100;
    
    private Queue<GameObject> availableBullets = new Queue<GameObject>();
    private List<GameObject> activeBullets = new List<GameObject>();
    
    private static BulletPool instance;
    public static BulletPool Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("BulletPool: bulletPrefab is not assigned!");
            return;
        }
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject bullet = CreateBullet();
            bullet.SetActive(false);
            availableBullets.Enqueue(bullet);
        }
    }
    
    private GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform);
        bullet.name = $"Bullet_{availableBullets.Count + activeBullets.Count}";
        return bullet;
    }
    
    /// <summary>
    /// Get a bullet from the pool. Returns null if pool is exhausted and max size reached.
    /// </summary>
    public GameObject GetBullet()
    {
        GameObject bullet;
        
        if (availableBullets.Count > 0)
        {
            bullet = availableBullets.Dequeue();
        }
        else if (activeBullets.Count + availableBullets.Count < maxPoolSize)
        {
            // Create new bullet if under max size
            bullet = CreateBullet();
        }
        else
        {
            // Pool exhausted - reuse oldest active bullet
            if (activeBullets.Count > 0)
            {
                bullet = activeBullets[0];
                activeBullets.RemoveAt(0);
                ReturnBullet(bullet); // Reset it first
                bullet = availableBullets.Dequeue();
            }
            else
            {
                Debug.LogWarning("BulletPool: Cannot get bullet - pool exhausted!");
                return null;
            }
        }
        
        // Reset bullet state before activating
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Reset();
        }
        
        bullet.SetActive(true);
        activeBullets.Add(bullet);
        return bullet;
    }
    
    /// <summary>
    /// Return a bullet to the pool. Call this instead of Destroy().
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;
        
        // Reset bullet state
        bullet.SetActive(false);
        bullet.transform.SetParent(transform);
        bullet.transform.position = Vector3.zero;
        bullet.transform.rotation = Quaternion.identity;
        
        // Reset Bullet component
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Reset();
        }
        
        // Reset Rigidbody
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Remove from active list and add to available
        activeBullets.Remove(bullet);
        availableBullets.Enqueue(bullet);
    }
    
    /// <summary>
    /// Clear all active bullets (useful for scene transitions, etc.).
    /// </summary>
    public void ClearAll()
    {
        foreach (GameObject bullet in activeBullets)
        {
            if (bullet != null)
            {
                ReturnBullet(bullet);
            }
        }
        activeBullets.Clear();
    }
}

