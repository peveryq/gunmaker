using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton manager for location transitions.
/// Handles location root GameObject enable/disable, weapon preservation, and session earnings tracking.
/// </summary>
public class LocationManager : MonoBehaviour
{
    public enum LocationType
    {
        Workshop,
        TestingRange
    }
    
    private static LocationManager instance;
    public static LocationManager Instance => instance;
    
    [Header("Location Roots")]
    [SerializeField] private GameObject workshopRoot;
    [SerializeField] private GameObject testingRangeRoot;
    
    [Header("References")]
    [SerializeField] private LoadingScreen loadingScreen;
    [SerializeField] private FadeScreen fadeScreen;
    [SerializeField] private InteractionHandler interactionHandler;
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private Transform workshopSpawnPoint;
    [SerializeField] private Transform testingRangeSpawnPoint;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInSpeed = 0.5f;
    
    private LocationType currentLocation = LocationType.Workshop;
    private ItemPickup savedWeapon;
    private WeaponState savedWeaponState;
    private EarningsTracker earningsTracker;
    
    private struct WeaponState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Transform parent;
        public bool wasEquipped;
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find or create earnings tracker
            earningsTracker = GetComponent<EarningsTracker>();
            if (earningsTracker == null)
            {
                earningsTracker = gameObject.AddComponent<EarningsTracker>();
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Find references if not assigned
        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }
        
        if (firstPersonController == null)
        {
            firstPersonController = FindFirstObjectByType<FirstPersonController>();
        }
        
        // Initialize locations
        if (workshopRoot != null)
        {
            workshopRoot.SetActive(true);
        }
        
        if (testingRangeRoot != null)
        {
            testingRangeRoot.SetActive(false);
        }
    }
    
    /// <summary>
    /// Transition to a specific location
    /// </summary>
    public void TransitionToLocation(LocationType location)
    {
        if (currentLocation == location)
        {
            Debug.LogWarning($"LocationManager: Already at {location}");
            return;
        }
        
        StartCoroutine(TransitionRoutine(location));
    }
    
    private IEnumerator TransitionRoutine(LocationType targetLocation)
    {
        LocationType previousLocation = currentLocation;
        
        // Save weapon state before transition
        SaveWeaponState();
        
        // Show loading screen
        if (loadingScreen != null)
        {
            bool loadingComplete = false;
            loadingScreen.StartLoading(() => loadingComplete = true);
            
            // Wait for loading to complete
            while (!loadingComplete)
            {
                yield return null;
            }
        }
        
        // Deactivate previous location
        DeactivateLocation(previousLocation);
        
        // Activate new location
        ActivateLocation(targetLocation);
        
        // Wait a frame for objects to initialize
        yield return null;
        
        // Move player to spawn point
        Transform spawnPoint = GetSpawnPoint(targetLocation);
        if (spawnPoint != null && firstPersonController != null)
        {
            firstPersonController.transform.position = spawnPoint.position;
            firstPersonController.transform.rotation = spawnPoint.rotation;
        }
        
        // Restore weapon if returning to workshop
        if (targetLocation == LocationType.Workshop)
        {
            RestoreWeapon();
        }
        
        // Fade in from loading screen
        if (loadingScreen != null && fadeScreen != null)
        {
            // Set fade screen to opaque first, then fade out loading screen and fade screen
            fadeScreen.SetFade(1f);
            loadingScreen.FadeOut(fadeInSpeed, () => {
                fadeScreen.FadeOut(fadeInSpeed);
            });
        }
        else if (fadeScreen != null)
        {
            // Set fade screen to opaque first, then fade out
            fadeScreen.SetFade(1f);
            fadeScreen.FadeOut(fadeInSpeed);
        }
        else if (loadingScreen != null)
        {
            // Just fade out loading screen if no fade screen
            loadingScreen.FadeOut(fadeInSpeed);
        }
        
        currentLocation = targetLocation;
        
        // Notify location change
        OnLocationChanged(targetLocation);
    }
    
    private void SaveWeaponState()
    {
        if (interactionHandler == null || interactionHandler.CurrentItem == null)
        {
            savedWeapon = null;
            return;
        }
        
        ItemPickup currentItem = interactionHandler.CurrentItem;
        
        // Only save if it's a weapon (has WeaponBody or WeaponController)
        WeaponBody weaponBody = currentItem.GetComponent<WeaponBody>();
        WeaponController weaponController = currentItem.GetComponent<WeaponController>();
        
        if (weaponBody == null && weaponController == null)
        {
            savedWeapon = null;
            return;
        }
        
        savedWeapon = currentItem;
        savedWeaponState.position = currentItem.transform.position;
        savedWeaponState.rotation = currentItem.transform.rotation;
        savedWeaponState.parent = currentItem.transform.parent;
        savedWeaponState.wasEquipped = true;
    }
    
    private void RestoreWeapon()
    {
        if (savedWeapon == null || interactionHandler == null)
        {
            return;
        }
        
        // Check if weapon still exists in scene
        if (savedWeapon == null || !savedWeapon.gameObject.activeInHierarchy)
        {
            // Weapon was destroyed or dropped - try to find it
            WeaponBody[] allWeapons = FindObjectsByType<WeaponBody>(FindObjectsSortMode.None);
            foreach (WeaponBody wb in allWeapons)
            {
                if (wb.name == savedWeapon.name || wb.WeaponName == savedWeapon.name)
                {
                    ItemPickup pickup = wb.GetComponent<ItemPickup>();
                    if (pickup != null)
                    {
                        savedWeapon = pickup;
                        break;
                    }
                }
            }
        }
        
        // If still not found, can't restore
        if (savedWeapon == null || !savedWeapon.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("LocationManager: Could not restore weapon - not found in scene");
            return;
        }
        
        // Force equip weapon to player
        if (interactionHandler != null)
        {
            // Drop current item if any
            if (interactionHandler.CurrentItem != null && interactionHandler.CurrentItem != savedWeapon)
            {
                interactionHandler.DropCurrentItem();
            }
            
            // Force pickup the saved weapon
            interactionHandler.ForcePickupItem(savedWeapon);
        }
    }
    
    private void ActivateLocation(LocationType location)
    {
        switch (location)
        {
            case LocationType.Workshop:
                if (workshopRoot != null)
                {
                    workshopRoot.SetActive(true);
                }
                break;
            case LocationType.TestingRange:
                if (testingRangeRoot != null)
                {
                    testingRangeRoot.SetActive(true);
                }
                break;
        }
    }
    
    private void DeactivateLocation(LocationType location)
    {
        switch (location)
        {
            case LocationType.Workshop:
                if (workshopRoot != null)
                {
                    workshopRoot.SetActive(false);
                }
                break;
            case LocationType.TestingRange:
                if (testingRangeRoot != null)
                {
                    testingRangeRoot.SetActive(false);
                }
                break;
        }
    }
    
    private Transform GetSpawnPoint(LocationType location)
    {
        switch (location)
        {
            case LocationType.Workshop:
                return workshopSpawnPoint;
            case LocationType.TestingRange:
                return testingRangeSpawnPoint;
            default:
                return null;
        }
    }
    
    private void OnLocationChanged(LocationType newLocation)
    {
        // This can be extended with events if needed
        Debug.Log($"LocationManager: Changed to {newLocation}");
    }
    
    /// <summary>
    /// Get current location
    /// </summary>
    public LocationType CurrentLocation => currentLocation;
    
    /// <summary>
    /// Get earnings tracker
    /// </summary>
    public EarningsTracker EarningsTracker => earningsTracker;
    
    /// <summary>
    /// Start earnings tracking (called when shooting timer starts)
    /// </summary>
    public void StartEarningsTracking()
    {
        if (earningsTracker != null)
        {
            earningsTracker.StartTracking();
        }
    }
    
    /// <summary>
    /// Stop earnings tracking and get total earnings (called when shooting timer ends)
    /// </summary>
    public int StopEarningsTracking()
    {
        if (earningsTracker != null)
        {
            return earningsTracker.StopTracking();
        }
        return 0;
    }
}

