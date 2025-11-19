using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

/// <summary>
/// Handles YG2 SDK initialization on startup scene.
/// Waits for SDK to initialize, then loads main scene.
/// NOT a singleton - lives only on bootstrap scene.
/// </summary>
public class Bootstrapper : MonoBehaviour
{
    [Header("Optional: Simple Loading Indicator")]
    [Tooltip("Optional simple UI GameObject to show while waiting for SDK. Can be left empty.")]
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Settings")]
    [Tooltip("How often to check if SDK is enabled (seconds)")]
    [SerializeField] private float checkInterval = 0.1f;
    
    [Tooltip("Maximum time to wait for SDK initialization (seconds). If exceeded, will load scene anyway.")]
    [SerializeField] private float maxWaitTime = 30f;
    
    private float waitStartTime;
    
    void Start()
    {
        waitStartTime = Time.realtimeSinceStartup;
        
        // Show optional loading indicator
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
        }
        
        StartCoroutine(WaitForSDK());
    }
    
    private IEnumerator WaitForSDK()
    {
        // Wait for YG2 SDK initialization
        while (!YG2.isSDKEnabled)
        {
            // Timeout check (in case of problems)
            if (Time.realtimeSinceStartup - waitStartTime > maxWaitTime)
            {
                Debug.LogWarning("Bootstrapper: YG2 SDK initialization timeout! Loading scene anyway...");
                // Load scene even without SDK (for debugging or fallback)
                break;
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
        
        // Hide loading indicator
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
        
        // SDK initialized, load main scene
        Debug.Log("Bootstrapper: YG2 SDK initialized. Loading main scene...");
        SceneManager.LoadScene(1); // Main.unity
    }
}
