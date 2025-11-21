using UnityEngine;

/// <summary>
/// Handles welding logic for interaction buttons and mobile input
/// </summary>
public class WeldingController : MonoBehaviour
{
    private static WeldingController instance;
    public static WeldingController Instance => instance;
    
    private Workbench currentWorkbench;
    private Blowtorch currentBlowtorch;
    private WeldingSystem currentWeldingTarget;
    private bool isWelding = false;
    private bool isKeyboardWelding = false; // Track if welding was started by keyboard
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Continue welding process if active
        if (isWelding && currentBlowtorch != null && currentWeldingTarget != null)
        {
            // Check if welding is complete
            if (currentWeldingTarget.IsWelded)
            {
                StopWelding();
                return;
            }
            
            if (currentBlowtorch.IsWorking)
            {
                float progressAdded = currentBlowtorch.WeldingSpeed * Time.deltaTime;
                float newProgress = currentWeldingTarget.AddWeldingProgress(progressAdded);
                
                // Show sparks during welding
                if (currentWorkbench != null)
                {
                    currentWorkbench.ShowWeldingSparks(currentWeldingTarget);
                }
                
                // Check if welding just completed
                if (currentWeldingTarget.IsWelded)
                {
                    StopWelding();
                }
            }
        }
    }
    
    /// <summary>
    /// Start welding process from interaction button
    /// </summary>
    public void StartWelding(Workbench workbench, Blowtorch blowtorch, WeldingSystem weldingTarget, bool fromKeyboard = false)
    {
        if (isWelding) return;
        
        currentWorkbench = workbench;
        currentBlowtorch = blowtorch;
        currentWeldingTarget = weldingTarget;
        isWelding = true;
        isKeyboardWelding = fromKeyboard;
        
        // Start blowtorch working
        if (currentBlowtorch != null && currentWorkbench != null)
        {
            // Get blowtorch work position from workbench
            Transform workPosition = currentWorkbench.GetBlowtorchWorkPosition();
            currentBlowtorch.StartWorking(workPosition);
            
            // Start sparks via workbench
            currentWorkbench.ShowWeldingSparks(currentWeldingTarget);
        }
    }
    
    /// <summary>
    /// Stop welding process
    /// </summary>
    public void StopWelding()
    {
        if (!isWelding) return;
        
        isWelding = false;
        
        // Stop blowtorch
        if (currentBlowtorch != null)
        {
            currentBlowtorch.StopWorking();
        }
        
        // Stop sparks via workbench
        if (currentWorkbench != null)
        {
            currentWorkbench.HideWeldingSparks();
        }
        
        // Clear references
        currentWorkbench = null;
        currentBlowtorch = null;
        currentWeldingTarget = null;
        isKeyboardWelding = false;
    }
    
    /// <summary>
    /// Check if welding is currently active
    /// </summary>
    public bool IsWelding => isWelding;
    
    /// <summary>
    /// Check if welding was started by keyboard input
    /// </summary>
    public bool IsKeyboardWelding => isKeyboardWelding;
    
    
    /// <summary>
    /// Setup welding context (called by workbench when player looks at it with blowtorch)
    /// </summary>
    public void SetupWeldingContext(Workbench workbench, Blowtorch blowtorch, WeldingSystem weldingTarget)
    {
        // Only update context if not currently welding
        if (!isWelding)
        {
            currentWorkbench = workbench;
            currentBlowtorch = blowtorch;
            currentWeldingTarget = weldingTarget;
        }
    }
    
    /// <summary>
    /// Clear welding context (called when player stops looking at workbench)
    /// </summary>
    public void ClearWeldingContext()
    {
        // Only clear if not actively welding
        if (!isWelding)
        {
            currentWorkbench = null;
            currentBlowtorch = null;
            currentWeldingTarget = null;
        }
    }
}
