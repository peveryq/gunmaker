using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Drop Containers")]
    [Tooltip("Container for dropped items in workshop. Should be child of workshopRoot.")]
    [SerializeField] private Transform workshopDropContainer;
    [Tooltip("Container for dropped items in testing range. Should be child of testingRangeRoot.")]
    [SerializeField] private Transform testingRangeDropContainer;
    
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
    
    private struct OriginalItemState
    {
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Transform originalParent;
        public bool shouldAlwaysReset; // For items like blowtorch that should reset even if held
    }
    
    private Dictionary<ItemPickup, OriginalItemState> registeredItems = new Dictionary<ItemPickup, OriginalItemState>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            
            // Move to root if parented (DontDestroyOnLoad only works for root objects)
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
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
    
    private Coroutine transitionCoroutine;
    
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
        
        // Stop any existing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        transitionCoroutine = StartCoroutine(TransitionRoutine(location));
    }
    
    private IEnumerator TransitionRoutine(LocationType targetLocation)
    {
        LocationType previousLocation = currentLocation;
        
        // Save weapon state before transition
        SaveWeaponState();
        
        // Set fade screen to opaque BEFORE starting loading (so it's black during load)
        if (fadeScreen != null)
        {
            fadeScreen.SetFade(1f);
        }
        
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
        
        // Move player to spawn point (with proper CharacterController handling)
        Transform spawnPoint = GetSpawnPoint(targetLocation);
        if (spawnPoint != null && firstPersonController != null)
        {
            SetPlayerPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
        
        // Restore weapon if returning to workshop
        if (targetLocation == LocationType.Workshop)
        {
            RestoreWeapon();
        }
        
        // Wait another frame to ensure everything is set up
        yield return null;
        
        // Reset items if returning to workshop (after location is activated)
        if (targetLocation == LocationType.Workshop)
        {
            ResetRegisteredItems();
            // Wait a frame after reset to ensure physics settle
            yield return null;
        }
        
        // Fade out from loading screen and fade screen
        if (loadingScreen != null && fadeScreen != null)
        {
            // Fade out loading screen first, then fade screen
            loadingScreen.FadeOut(fadeInSpeed, () => {
                fadeScreen.FadeOut(fadeInSpeed);
            });
        }
        else if (fadeScreen != null)
        {
            // Just fade out fade screen
            fadeScreen.FadeOut(fadeInSpeed);
        }
        else if (loadingScreen != null)
        {
            // Just fade out loading screen if no fade screen
            loadingScreen.FadeOut(fadeInSpeed);
        }
        
        currentLocation = targetLocation;
        transitionCoroutine = null;
        
        // Notify location change
        OnLocationChanged(targetLocation);
    }
    
    /// <summary>
    /// Get drop container for current location
    /// </summary>
    public Transform GetDropContainerForCurrentLocation()
    {
        switch (currentLocation)
        {
            case LocationType.Workshop:
                return workshopDropContainer;
            case LocationType.TestingRange:
                return testingRangeDropContainer;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Register an item to be reset to its original position when returning to workshop
    /// </summary>
    public void RegisterItemForReset(ItemPickup item, bool alwaysReset = false)
    {
        if (item == null) return;
        
        OriginalItemState state = new OriginalItemState
        {
            originalPosition = item.transform.position,
            originalRotation = item.transform.rotation,
            originalParent = item.transform.parent,
            shouldAlwaysReset = alwaysReset
        };
        
        registeredItems[item] = state;
    }
    
    /// <summary>
    /// Unregister an item from reset tracking
    /// </summary>
    public void UnregisterItemForReset(ItemPickup item)
    {
        if (item != null && registeredItems.ContainsKey(item))
        {
            registeredItems.Remove(item);
        }
    }
    
    private void ResetRegisteredItems()
    {
        if (registeredItems.Count == 0) return;
        
        List<ItemPickup> itemsToReset = new List<ItemPickup>(registeredItems.Keys);
        
        foreach (ItemPickup item in itemsToReset)
        {
            if (item == null) continue;
            
            OriginalItemState state = registeredItems[item];
            
            // Check if item should be reset
            bool shouldReset = false;
            
            if (state.shouldAlwaysReset)
            {
                // Always reset items like blowtorch
                // If held, drop it first
                if (item.IsHeld && interactionHandler != null && interactionHandler.CurrentItem == item)
                {
                    interactionHandler.DropCurrentItem();
                }
                shouldReset = true;
            }
            else
            {
                // Only reset if not currently held
                if (!item.IsHeld)
                {
                    shouldReset = true;
                }
            }
            
            if (shouldReset)
            {
                // Stop blowtorch working state FIRST (before any position changes)
                Blowtorch blowtorch = item.GetComponent<Blowtorch>();
                if (blowtorch != null)
                {
                    // Force stop working immediately
                    blowtorch.StopWorking();
                    
                    // Also ensure audio is stopped (in case it's on a child object)
                    AudioSource[] allAudioSources = item.GetComponentsInChildren<AudioSource>();
                    foreach (AudioSource audioSource in allAudioSources)
                    {
                        if (audioSource != null && audioSource.isPlaying)
                        {
                            audioSource.Stop();
                            audioSource.clip = null;
                        }
                    }
                }
                
                // Reset to original position
                item.transform.position = state.originalPosition;
                item.transform.rotation = state.originalRotation;
                
                // Restore original parent if it exists and is active
                if (state.originalParent != null && state.originalParent.gameObject.activeInHierarchy)
                {
                    item.transform.SetParent(state.originalParent);
                }
                else
                {
                    // If original parent is not available, parent to appropriate drop container
                    Transform dropContainer = GetDropContainerForCurrentLocation();
                    if (dropContainer != null)
                    {
                        item.transform.SetParent(dropContainer);
                    }
                }
                
                // Disable physics if item has Rigidbody
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Reset velocities first (before setting kinematic)
                    bool wasKinematic = rb.isKinematic;
                    if (!wasKinematic)
                    {
#if UNITY_6000_0_OR_NEWER
                        rb.linearVelocity = Vector3.zero;
#else
                        rb.velocity = Vector3.zero;
#endif
                        rb.angularVelocity = Vector3.zero;
                    }
                    
                    // Then set kinematic and disable gravity
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }
    }
    
    private void SetPlayerPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        if (firstPersonController == null) return;
        
        CharacterController cc = firstPersonController.GetComponent<CharacterController>();
        bool wasEnabled = cc != null && cc.enabled;
        
        // Extract rotation angles from spawn point
        Vector3 eulerAngles = rotation.eulerAngles;
        float horizontalRotation = eulerAngles.y; // Y rotation for horizontal (player body)
        float verticalRotation = eulerAngles.x; // X rotation for vertical (camera look up/down)
        
        // Clamp vertical rotation to valid range (convert 0-360 to -180-180)
        if (verticalRotation > 180f)
        {
            verticalRotation -= 360f;
        }
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        
        // Set rotation first (before position to avoid issues)
        firstPersonController.SetRotation(horizontalRotation, verticalRotation);
        
        // Set position - use CharacterController's internal position if available
        if (cc != null)
        {
            // CharacterController needs special handling for position
            // Disable it, set transform position, then re-enable
            cc.enabled = false;
            firstPersonController.transform.position = position;
            
            // Re-enable if it was enabled before
            if (wasEnabled)
            {
                cc.enabled = true;
                // Force update by moving zero distance (syncs internal position)
                cc.Move(Vector3.zero);
            }
        }
        else
        {
            // No CharacterController, just set position directly
            firstPersonController.transform.position = position;
        }
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

