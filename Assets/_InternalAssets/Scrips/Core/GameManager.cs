using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

/// <summary>
/// Main game manager - coordinates all systems initialization.
/// Lives on main scene, survives scene reloads via DontDestroyOnLoad.
/// All system initializations are optional - if system doesn't exist, it's skipped.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;
    
    [Header("References")]
    [Tooltip("LoadingScreen to show during game initialization. Will be found automatically if not assigned.")]
    [SerializeField] private LoadingScreen loadingScreen;
    
    [Header("Initialization Settings")]
    [Tooltip("Delay after all systems initialized before hiding loading screen (seconds)")]
    [SerializeField] private float initializationDelay = 0.1f;
    
    [Tooltip("Fade out duration for loading screen (seconds)")]
    [SerializeField] private float loadingScreenFadeDuration = 0.5f;
    
    [Tooltip("Maximum time to wait for save data loading (seconds). If exceeded, will continue anyway.")]
    [SerializeField] private float maxSaveLoadWaitTime = 10f;
    
    [Tooltip("Maximum time to wait for location to fully load (seconds). If exceeded, will continue anyway.")]
    [SerializeField] private float maxLocationLoadWaitTime = 5f;
    
    // State
    private bool isInitialized = false;
    private bool isInitializing = false; // Track if initialization is in progress
    public bool IsInitialized => isInitialized;
    
    // Events
    public System.Action OnGameInitialized;
    
    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Debug.LogWarning("GameManager: Another instance exists. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Find LoadingScreen if not assigned
        if (loadingScreen == null)
        {
            loadingScreen = FindFirstObjectByType<LoadingScreen>();
            if (loadingScreen == null)
            {
                Debug.LogWarning("GameManager: LoadingScreen not found. Game will initialize without loading screen.");
            }
        }
    }
    
    void Start()
    {
        StartCoroutine(InitializeGame());
    }
    
    void OnEnable()
    {
        // Subscribe to scene loaded event to handle scene reloads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// Called when a scene is loaded. Restarts initialization if Main scene is loaded and not already initialized.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If Main scene is loaded and we haven't initialized yet (or were reset), restart initialization
        // This handles the case when scene is reloaded (e.g., after clearing saves)
        if (scene.buildIndex == 1 && !isInitialized && !isInitializing) // Main.unity is scene index 1
        {
            Debug.Log("GameManager: Main scene loaded, restarting initialization...");
            // Re-find LoadingScreen in case it was recreated in the scene
            if (loadingScreen == null)
            {
                loadingScreen = FindFirstObjectByType<LoadingScreen>();
            }
            StartCoroutine(InitializeGame());
        }
    }
    
    /// <summary>
    /// Reset GameManager state (call when save data is cleared)
    /// Resets initialization flags so game will reinitialize on next scene load
    /// </summary>
    public void ResetGameState()
    {
        Debug.Log("GameManager: Resetting game state...");
        isInitialized = false;
        isInitializing = false;
        Debug.Log("GameManager: Game state reset complete. Will reinitialize on next scene load.");
    }
    
    /// <summary>
    /// Main initialization coroutine. Initializes all systems in order.
    /// All systems are optional - if they don't exist, initialization is skipped.
    /// Waits for location and save data to fully load before completing.
    /// </summary>
    private IEnumerator InitializeGame()
    {
        // Prevent multiple initialization coroutines from running simultaneously
        if (isInitializing)
        {
            Debug.Log("GameManager: Initialization already in progress, skipping...");
            yield break;
        }
        
        isInitializing = true;
        isInitialized = false;
        
        Debug.Log("GameManager: Starting game initialization...");
        
        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.StartLoading();
        }
        
        // Wait a frame for all objects to initialize
        yield return null;
        
        // Initialize systems in order (all optional)
        yield return InitializeDeviceDetection();
        yield return InitializeLocalization();
        yield return InitializeSettings();
        yield return InitializeMobileInput();
        yield return InitializeWeldingController();
        yield return InitializeAdManager();
        yield return InitializeTutorial();
        
        // Wait for LocationManager to be ready
        yield return WaitForLocationManager();
        
        // Wait for save system to load data
        yield return WaitForSaveSystemLoad();
        
        // Wait for location to be fully loaded (all objects active)
        yield return WaitForLocationFullyLoaded();
        
        // Wait a bit for all systems to be ready
        yield return new WaitForSeconds(initializationDelay);
        
        // Mark as initialized
        isInitialized = true;
        isInitializing = false;
        
        Debug.Log("GameManager: All systems initialized and location loaded!");
        
        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.FadeOut(loadingScreenFadeDuration);
        }
        
        // Notify other systems
        OnGameInitialized?.Invoke();
    }
    
    /// <summary>
    /// Initialize DeviceDetectionManager.
    /// DeviceDetectionManager is now part of the project, so we initialize it directly.
    /// </summary>
    private IEnumerator InitializeDeviceDetection()
    {
        // Wait a frame for DeviceDetectionManager to initialize
        yield return null;
        
        var deviceManager = DeviceDetectionManager.Instance;
        if (deviceManager != null)
        {
            Debug.Log("GameManager: DeviceDetectionManager found and initialized.");
            
            // Force refresh detection if YG2 is ready
            if (YG2.isSDKEnabled)
            {
                deviceManager.RefreshDeviceDetection();
            }
        }
        else
        {
            Debug.LogWarning("GameManager: DeviceDetectionManager not found. Device detection will not work.");
        }
    }
    
    /// <summary>
    /// Initialize LocalizationManager.
    /// LocalizationManager is now part of the project, so we initialize it directly.
    /// </summary>
    private IEnumerator InitializeLocalization()
    {
        // Wait a frame for LocalizationManager to initialize
        yield return null;
        
        var locManager = LocalizationManager.Instance;
        if (locManager != null)
        {
            Debug.Log("GameManager: LocalizationManager found and initialized.");
            
            // If YG2 wasn't ready when LocalizationManager started, reload language
            if (YG2.isSDKEnabled)
            {
                locManager.ReloadLanguageFromYG2();
            }
        }
        else
        {
            Debug.LogWarning("GameManager: LocalizationManager not found. Localization will not work.");
        }
    }
    
    /// <summary>
    /// Initialize SettingsManager.
    /// SettingsManager handles game settings loading/saving and applying.
    /// </summary>
    private IEnumerator InitializeSettings()
    {
        Debug.Log("GameManager: Initializing Settings...");
        
        // SettingsManager is a singleton that initializes itself
        // We just need to ensure it exists
        if (SettingsManager.Instance == null)
        {
            GameObject settingsGO = new GameObject("SettingsManager");
            settingsGO.AddComponent<SettingsManager>();
            DontDestroyOnLoad(settingsGO);
        }
        
        yield return null;
        Debug.Log("GameManager: Settings initialized.");
    }
    
    /// <summary>
    /// Initialize MobileInputManager.
    /// MobileInputManager is now part of the project, so we initialize it directly.
    /// </summary>
    private IEnumerator InitializeMobileInput()
    {
        // Wait a frame for MobileInputManager to initialize
        yield return null;
        
        var mobileManager = MobileInputManager.Instance;
        if (mobileManager != null)
        {
            Debug.Log("GameManager: MobileInputManager found and initialized.");
        }
        else
        {
            Debug.LogWarning("GameManager: MobileInputManager not found. Mobile input will not work.");
        }
    }
    
    /// <summary>
    /// Initialize WeldingController (optional).
    /// Creates WeldingController instance if it doesn't exist.
    /// </summary>
    private IEnumerator InitializeWeldingController()
    {
        if (WeldingController.Instance == null)
        {
            GameObject weldingControllerObj = new GameObject("WeldingController");
            weldingControllerObj.AddComponent<WeldingController>();
            Debug.Log("GameManager: WeldingController created and initialized.");
        }
        else
        {
            Debug.Log("GameManager: WeldingController already exists.");
        }
        yield return null;
    }
    
    /// <summary>
    /// Initialize AdManager (optional).
    /// If AdManager doesn't exist, this is skipped.
    /// Uses reflection to safely check if class exists.
    /// </summary>
    private IEnumerator InitializeAdManager()
    {
        var adManagerType = System.Type.GetType("AdManager");
        if (adManagerType != null)
        {
            var instanceProperty = adManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProperty != null)
            {
                var instance = instanceProperty.GetValue(null);
                if (instance != null)
                {
                    Debug.Log("GameManager: AdManager found and initialized.");
                }
            }
        }
        yield return null;
    }
    
    /// <summary>
    /// Initialize TutorialManager (optional).
    /// TutorialManager initializes itself, we just ensure it exists.
    /// </summary>
    private IEnumerator InitializeTutorial()
    {
        // Wait a frame for TutorialManager to initialize
        yield return null;
        
        var tutorialManager = TutorialManager.Instance;
        if (tutorialManager != null)
        {
            Debug.Log("GameManager: TutorialManager found and initialized.");
        }
        else
        {
            Debug.Log("GameManager: TutorialManager not found. Tutorial system will not be available.");
        }
    }
    
    /// <summary>
    /// Wait for LocationManager to be initialized
    /// </summary>
    private IEnumerator WaitForLocationManager()
    {
        float waitStartTime = Time.realtimeSinceStartup;
        
        while (LocationManager.Instance == null)
        {
            if (Time.realtimeSinceStartup - waitStartTime > maxLocationLoadWaitTime)
            {
                Debug.LogWarning("GameManager: LocationManager not found after timeout. Continuing anyway...");
                yield break;
            }
            yield return null;
        }
        
        Debug.Log("GameManager: LocationManager found and ready.");
    }
    
    /// <summary>
    /// Wait for SaveSystemManager to complete loading save data
    /// </summary>
    private IEnumerator WaitForSaveSystemLoad()
    {
        var saveManager = SaveSystemManager.Instance;
        if (saveManager == null)
        {
            Debug.Log("GameManager: SaveSystemManager not found. Skipping save load wait.");
            yield break;
        }
        
        Debug.Log("GameManager: Waiting for save data to load...");
        
        float waitStartTime = Time.realtimeSinceStartup;
        bool loadComplete = false;
        
        // Subscribe to load complete event
        System.Action onLoadComplete = () => { loadComplete = true; };
        saveManager.OnLoadComplete += onLoadComplete;
        
        // Check if already loaded
        if (saveManager.IsLoadComplete)
        {
            loadComplete = true;
        }
        
        // Wait for load to complete or timeout
        while (!loadComplete)
        {
            if (Time.realtimeSinceStartup - waitStartTime > maxSaveLoadWaitTime)
            {
                Debug.LogWarning("GameManager: Save data loading timeout. Continuing anyway...");
                break;
            }
            yield return null;
        }
        
        // Unsubscribe
        saveManager.OnLoadComplete -= onLoadComplete;
        
        if (loadComplete)
        {
            Debug.Log("GameManager: Save data loading completed.");
        }
    }
    
    /// <summary>
    /// Wait for location to be fully loaded (all objects active and ready)
    /// </summary>
    private IEnumerator WaitForLocationFullyLoaded()
    {
        var locationManager = LocationManager.Instance;
        if (locationManager == null)
        {
            yield break;
        }
        
        Debug.Log("GameManager: Waiting for location to fully load...");
        
        float waitStartTime = Time.realtimeSinceStartup;
        
        // Wait a few frames for all objects to initialize and become active
        yield return null;
        yield return null;
        yield return null;
        
        // Additional wait to ensure all save-loaded objects are spawned and active
        yield return new WaitForSeconds(0.2f);
        
        Debug.Log("GameManager: Location fully loaded.");
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

