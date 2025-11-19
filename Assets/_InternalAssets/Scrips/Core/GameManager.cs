using System.Collections;
using UnityEngine;
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
    
    // State
    private bool isInitialized = false;
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
            loadingScreen = FindObjectOfType<LoadingScreen>();
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
    
    /// <summary>
    /// Main initialization coroutine. Initializes all systems in order.
    /// All systems are optional - if they don't exist, initialization is skipped.
    /// </summary>
    private IEnumerator InitializeGame()
    {
        Debug.Log("GameManager: Starting game initialization...");
        
        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.StartLoading();
        }
        
        // Wait a frame for all objects to initialize
        yield return null;
        
        // Initialize systems in order (all optional)
        yield return InitializeLocalization();
        yield return InitializeMobileInput();
        yield return InitializeAdManager();
        yield return InitializeSaveSystem();
        
        // Wait a bit for all systems to be ready
        yield return new WaitForSeconds(initializationDelay);
        
        // Mark as initialized
        isInitialized = true;
        
        Debug.Log("GameManager: All systems initialized!");
        
        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.FadeOut(loadingScreenFadeDuration);
        }
        
        // Notify other systems
        OnGameInitialized?.Invoke();
    }
    
    /// <summary>
    /// Initialize LocalizationManager (optional).
    /// If LocalizationManager doesn't exist, this is skipped.
    /// Uses reflection to safely check if class exists.
    /// </summary>
    private IEnumerator InitializeLocalization()
    {
        // Safe check using reflection - if system doesn't exist, just skip
        var locManagerType = System.Type.GetType("LocalizationManager");
        if (locManagerType != null)
        {
            var instanceProperty = locManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProperty != null)
            {
                var instance = instanceProperty.GetValue(null);
                if (instance != null)
                {
                    Debug.Log("GameManager: LocalizationManager found and initialized.");
                }
            }
        }
        yield return null;
    }
    
    /// <summary>
    /// Initialize MobileInputManager (optional).
    /// If MobileInputManager doesn't exist, this is skipped.
    /// Uses reflection to safely check if class exists.
    /// </summary>
    private IEnumerator InitializeMobileInput()
    {
        var mobileManagerType = System.Type.GetType("MobileInputManager");
        if (mobileManagerType != null)
        {
            var instanceProperty = mobileManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProperty != null)
            {
                var instance = instanceProperty.GetValue(null);
                if (instance != null)
                {
                    Debug.Log("GameManager: MobileInputManager found and initialized.");
                }
            }
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
    /// Initialize SaveSystemManager (optional).
    /// SaveSystemManager already exists in project, but we check for safety.
    /// </summary>
    private IEnumerator InitializeSaveSystem()
    {
        var saveManager = SaveSystemManager.Instance;
        if (saveManager != null)
        {
            Debug.Log("GameManager: SaveSystemManager found and initialized.");
            yield return null;
        }
        // If doesn't exist - just continue
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

