using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponNameInputUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TextMeshProUGUI promptText;
    
    private System.Action<string> onNameConfirmed;
    private System.Action onCancelled;
    private bool isActive = false;
    private FirstPersonController fpsController;
    private bool wasFpsControllerEnabled = false;
    
    private void Start()
    {
        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
        
        // Find FirstPersonController
        fpsController = FindFirstObjectByType<FirstPersonController>();
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Confirm on Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmName();
        }
        
        // Cancel on Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
    }
    
    public void ShowInputUI(string prompt, System.Action<string> onConfirmed, System.Action onCancel)
    {
        isActive = true;
        onNameConfirmed = onConfirmed;
        onCancelled = onCancel;
        
        if (inputPanel != null)
        {
            inputPanel.SetActive(true);
        }
        
        if (promptText != null)
        {
            promptText.text = prompt;
        }
        
        if (nameInputField != null)
        {
            nameInputField.text = "";
            nameInputField.ActivateInputField();
            nameInputField.Select();
        }
        
        // Unlock cursor for input
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable FPS controller to prevent movement/camera rotation
        if (fpsController != null)
        {
            wasFpsControllerEnabled = fpsController.enabled;
            fpsController.enabled = false;
        }
    }
    
    public void HideInputUI()
    {
        isActive = false;
        
        if (inputPanel != null)
        {
            inputPanel.SetActive(false);
        }
        
        // Re-lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable FPS controller
        if (fpsController != null && wasFpsControllerEnabled)
        {
            fpsController.enabled = true;
        }
        
        onNameConfirmed = null;
        onCancelled = null;
    }
    
    private void ConfirmName()
    {
        if (nameInputField == null) return;
        
        string weaponName = nameInputField.text.Trim();
        
        // Only confirm if at least one character entered
        if (weaponName.Length > 0)
        {
            onNameConfirmed?.Invoke(weaponName);
            HideInputUI();
        }
    }
    
    private void Cancel()
    {
        onCancelled?.Invoke();
        HideInputUI();
    }
    
    // Public method for UI Button (optional)
    public void OnConfirmButtonClicked()
    {
        ConfirmName();
    }
    
    // Public method for UI Button (optional)
    public void OnCancelButtonClicked()
    {
        Cancel();
    }
}

