using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages bullet hole decals with object pooling and limits
/// WebGL-optimized: reuses objects, limits max count
/// </summary>
public class BulletHoleManager : MonoBehaviour
{
    [Header("Decal Settings")]
    [SerializeField] private GameObject bulletHolePrefab; // Prefab with decal mesh
    [SerializeField] private int maxBulletHoles = 50; // Maximum number of bullet holes
    [SerializeField] private float bulletHoleLifetime = 10f; // How long before destroying
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem impactParticles; // Impact debris particles
    
    private Queue<BulletHole> activeBulletHoles = new Queue<BulletHole>();
    private Queue<GameObject> pooledBulletHoles = new Queue<GameObject>();
    
    private static BulletHoleManager instance;
    public static BulletHoleManager Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Spawn a bullet hole at hit point
    /// </summary>
    public void SpawnBulletHole(Vector3 position, Vector3 normal, Transform parent = null)
    {
        if (bulletHolePrefab == null) return;
        
        GameObject holeObj = GetPooledObject();
        
        if (holeObj == null)
        {
            // Pool exhausted, reuse oldest hole
            if (activeBulletHoles.Count >= maxBulletHoles)
            {
                BulletHole oldest = activeBulletHoles.Dequeue();
                if (oldest != null && oldest.gameObject != null)
                {
                    holeObj = oldest.gameObject;
                    oldest.Reset();
                }
            }
            else
            {
                // Create new object
                holeObj = Instantiate(bulletHolePrefab);
            }
        }
        
        if (holeObj == null) return;
        
        // CRITICAL: Detach from old parent first to reset scale inheritance
        holeObj.transform.SetParent(null);
        
        // Reset to prefab scale (get from prefab)
        Vector3 prefabScale = bulletHolePrefab.transform.localScale;
        holeObj.transform.localScale = prefabScale;
        
        // Position and rotation in world space
        holeObj.transform.position = position + normal * 0.01f; // Slight offset to prevent z-fighting
        holeObj.transform.rotation = Quaternion.LookRotation(-normal); // Look TOWARDS wall (invert normal)
        
        // Now parent with world position stays, and compensate for parent scale
        if (parent != null)
        {
            Vector3 parentScale = parent.lossyScale;
            holeObj.transform.SetParent(parent, true); // worldPositionStays = true
            
            // Compensate parent scale to maintain world scale = prefabScale
            holeObj.transform.localScale = new Vector3(
                prefabScale.x / parentScale.x,
                prefabScale.y / parentScale.y,
                prefabScale.z / parentScale.z
            );
        }
        else
        {
            // No parent - just use prefab scale
            holeObj.transform.localScale = prefabScale;
        }
        
        holeObj.SetActive(true);
        
        // Setup BulletHole component
        BulletHole bulletHole = holeObj.GetComponent<BulletHole>();
        if (bulletHole == null)
        {
            bulletHole = holeObj.AddComponent<BulletHole>();
        }
        
        bulletHole.Initialize(bulletHoleLifetime, () => ReturnToPool(holeObj));
        activeBulletHoles.Enqueue(bulletHole);
        
        // Spawn impact particles
        if (impactParticles != null)
        {
            impactParticles.transform.position = position;
            impactParticles.transform.rotation = Quaternion.LookRotation(normal);
            impactParticles.Play();
        }
    }
    
    private GameObject GetPooledObject()
    {
        if (pooledBulletHoles.Count > 0)
        {
            return pooledBulletHoles.Dequeue();
        }
        return null;
    }
    
    private void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pooledBulletHoles.Enqueue(obj);
    }
    
    /// <summary>
    /// Clear all bullet holes immediately
    /// </summary>
    public void ClearAllBulletHoles()
    {
        foreach (var hole in activeBulletHoles)
        {
            if (hole != null && hole.gameObject != null)
            {
                ReturnToPool(hole.gameObject);
            }
        }
        activeBulletHoles.Clear();
    }
}

