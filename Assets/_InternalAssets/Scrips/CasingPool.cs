using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for bullet casings to reduce Instantiate/Destroy calls.
/// WebGL-optimized: reuses casing GameObjects instead of creating/destroying them.
/// Especially important for high fire rate weapons.
/// </summary>
public class CasingPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxPoolSize = 200;
    
    private Queue<GameObject> availableCasings = new Queue<GameObject>();
    private List<GameObject> activeCasings = new List<GameObject>();
    
    private static CasingPool instance;
    public static CasingPool Instance => instance;
    
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
        if (casingPrefab == null)
        {
            Debug.LogWarning("CasingPool: casingPrefab is not assigned!");
            return;
        }
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject casing = CreateCasing();
            casing.SetActive(false);
            availableCasings.Enqueue(casing);
        }
    }
    
    private GameObject CreateCasing()
    {
        GameObject casing = Instantiate(casingPrefab, transform);
        casing.name = $"Casing_{availableCasings.Count + activeCasings.Count}";
        return casing;
    }
    
    /// <summary>
    /// Get a casing from the pool. Returns null if pool is exhausted and max size reached.
    /// </summary>
    public GameObject GetCasing()
    {
        GameObject casing;
        
        if (availableCasings.Count > 0)
        {
            casing = availableCasings.Dequeue();
        }
        else if (activeCasings.Count + availableCasings.Count < maxPoolSize)
        {
            // Create new casing if under max size
            casing = CreateCasing();
        }
        else
        {
            // Pool exhausted - reuse oldest active casing
            if (activeCasings.Count > 0)
            {
                casing = activeCasings[0];
                activeCasings.RemoveAt(0);
                ReturnCasing(casing); // Reset it first
                casing = availableCasings.Dequeue();
            }
            else
            {
                Debug.LogWarning("CasingPool: Cannot get casing - pool exhausted!");
                return null;
            }
        }
        
        // Reset casing state before activating
        BulletCasing casingScript = casing.GetComponent<BulletCasing>();
        if (casingScript != null)
        {
            casingScript.Reset();
        }
        
        casing.SetActive(true);
        activeCasings.Add(casing);
        return casing;
    }
    
    /// <summary>
    /// Return a casing to the pool. Call this instead of Destroy().
    /// This should be called by BulletCasing when it's lifetime expires.
    /// </summary>
    public void ReturnCasing(GameObject casing)
    {
        if (casing == null) return;
        
        // Reset casing state
        casing.SetActive(false);
        casing.transform.SetParent(transform);
        casing.transform.position = Vector3.zero;
        casing.transform.rotation = Quaternion.identity;
        casing.transform.localScale = Vector3.one;
        
        // Reset Rigidbody
        Rigidbody rb = casing.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
        
        // Reset BulletCasing component state
        BulletCasing casingScript = casing.GetComponent<BulletCasing>();
        if (casingScript != null)
        {
            casingScript.Reset();
        }
        
        // Remove from active list and add to available
        activeCasings.Remove(casing);
        availableCasings.Enqueue(casing);
    }
    
    /// <summary>
    /// Clear all active casings (useful for scene transitions, etc.).
    /// </summary>
    public void ClearAll()
    {
        foreach (GameObject casing in activeCasings)
        {
            if (casing != null)
            {
                ReturnCasing(casing);
            }
        }
        activeCasings.Clear();
    }
}

