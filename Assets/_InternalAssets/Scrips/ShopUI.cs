using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main shop UI controller with fullscreen layout
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject shopPanel;
    
    [Header("Category Buttons")]
    [SerializeField] private Button stocksButton;
    [SerializeField] private Button barrelsButton;
    [SerializeField] private Button magazinesButton;
    [SerializeField] private Button scopesButton;
    [SerializeField] private Button lasersButton; // Locked
    [SerializeField] private Button foregripsButton; // Locked
    
    [Header("Category Scroll")]
    [SerializeField] private ScrollRect categoryScrollRect;

    [Header("Category Button Visuals")]
    [SerializeField] private GameObject stocksSelectedIndicator;
    [SerializeField] private GameObject barrelsSelectedIndicator;
    [SerializeField] private GameObject magazinesSelectedIndicator;
    [SerializeField] private GameObject scopesSelectedIndicator;
    
    [Header("Category Button Texts")]
    [SerializeField] private TextMeshProUGUI stocksButtonText;
    [SerializeField] private TextMeshProUGUI barrelsButtonText;
    [SerializeField] private TextMeshProUGUI magazinesButtonText;
    [SerializeField] private TextMeshProUGUI scopesButtonText;
    
    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = new Color(1f, 0.55f, 0f); // Orange
    
    [Header("Header Bar")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;
    
    [Header("Item Grid")]
    [SerializeField] private Transform itemGridContainer;
    [SerializeField] private GameObject itemTilePrefab;
    [SerializeField] private ScrollRect itemScrollRect;
    
    [Header("References")]
    [SerializeField] private ShopOfferingGenerator offeringGenerator;
    [SerializeField] private PurchaseConfirmationUI purchaseConfirmationUI;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    
    private PartType currentCategory = PartType.Barrel;
    private List<ShopItemTile> currentTiles = new List<ShopItemTile>();
    private FirstPersonController fpsController;
    private bool wasControllerEnabled;
    private Button currentSelectedButton;
    private Coroutine scrollResetRoutine;
    
    private void Awake()
    {
        // Find FirstPersonController immediately (before any UI interaction)
        fpsController = FindFirstObjectByType<FirstPersonController>();
        
        // Setup button listeners
        if (stocksButton != null)
            stocksButton.onClick.AddListener(() => SwitchCategory(PartType.Stock));
        
        if (barrelsButton != null)
            barrelsButton.onClick.AddListener(() => SwitchCategory(PartType.Barrel));
        
        if (magazinesButton != null)
            magazinesButton.onClick.AddListener(() => SwitchCategory(PartType.Magazine));
        
        if (scopesButton != null)
            scopesButton.onClick.AddListener(() => SwitchCategory(PartType.Scope));
        
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
        
        // Setup audio
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.volume = 0.5f;
        }
        
        // Hide shop initially
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
    
    private void Update()
    {
        // Keep category button selected even if user clicks elsewhere
        if (currentSelectedButton != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != currentSelectedButton.gameObject)
            {
                currentSelectedButton.Select();
            }
        }
        
        // Close shop on ESC key
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
        {
            CloseShop();
        }
    }
    
    private void Start()
    {
        // Subscribe to money changes
        if (MoneySystem.Instance != null)
        {
            MoneySystem.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            UpdateMoneyDisplay(MoneySystem.Instance.CurrentMoney);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from money changes
        if (MoneySystem.Instance != null)
        {
            MoneySystem.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }
    
    /// <summary>
    /// Open the shop UI
    /// </summary>
    public void OpenShop()
    {
        // Try to find controller again if not found in Awake
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<FirstPersonController>();
            if (fpsController == null)
            {
                Debug.LogWarning("FirstPersonController not found! Player movement won't be disabled.");
            }
        }
        
        // Show shop panel
        if (shopPanel != null)
            shopPanel.SetActive(true);
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable FirstPersonController
        if (fpsController != null)
        {
            wasControllerEnabled = fpsController.enabled;
            fpsController.enabled = false;
        }
        
        ResetCategoryScrollPosition();
        
        // Populate with default category
        SwitchCategory(currentCategory);
    }
    
    /// <summary>
    /// Close the shop UI
    /// </summary>
    public void CloseShop()
    {
        // Hide shop panel
        if (shopPanel != null)
            shopPanel.SetActive(false);
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable FirstPersonController
        if (fpsController != null && wasControllerEnabled)
        {
            fpsController.enabled = true;
        }
        
        PlayButtonSound();
    }
    
    /// <summary>
    /// Switch to a different category
    /// </summary>
    public void SwitchCategory(PartType partType)
    {
        currentCategory = partType;
        
        // Update category button visuals
        UpdateCategoryButtons();
        
        // Refresh category display
        RefreshCategory();
        
        PlayButtonSound();
    }
    
    /// <summary>
    /// Update category button selected states
    /// </summary>
    private void UpdateCategoryButtons()
    {
        // Hide all indicators and reset text colors
        SetButtonState(stocksButton, stocksButtonText, stocksSelectedIndicator, false);
        SetButtonState(barrelsButton, barrelsButtonText, barrelsSelectedIndicator, false);
        SetButtonState(magazinesButton, magazinesButtonText, magazinesSelectedIndicator, false);
        SetButtonState(scopesButton, scopesButtonText, scopesSelectedIndicator, false);
        
        // Select button for current category
        Button selectedButton = null;
        TextMeshProUGUI selectedText = null;
        GameObject selectedIndicator = null;
        
        switch (currentCategory)
        {
            case PartType.Stock:
                selectedButton = stocksButton;
                selectedText = stocksButtonText;
                selectedIndicator = stocksSelectedIndicator;
                break;
            case PartType.Barrel:
                selectedButton = barrelsButton;
                selectedText = barrelsButtonText;
                selectedIndicator = barrelsSelectedIndicator;
                break;
            case PartType.Magazine:
                selectedButton = magazinesButton;
                selectedText = magazinesButtonText;
                selectedIndicator = magazinesSelectedIndicator;
                break;
            case PartType.Scope:
                selectedButton = scopesButton;
                selectedText = scopesButtonText;
                selectedIndicator = scopesSelectedIndicator;
                break;
        }
        
        // Apply selected state
        SetButtonState(selectedButton, selectedText, selectedIndicator, true);
        
        // Store current selected button for Update() to maintain selection
        currentSelectedButton = selectedButton;
    }
    
    /// <summary>
    /// Set button visual state (text color and indicator)
    /// </summary>
    private void SetButtonState(Button button, TextMeshProUGUI buttonText, GameObject indicator, bool isSelected)
    {
        // Set text color
        if (buttonText != null)
        {
            buttonText.color = isSelected ? selectedTextColor : normalTextColor;
        }
        
        // Set indicator visibility
        if (indicator != null)
        {
            indicator.SetActive(isSelected);
        }
        
        // Apply Unity's selection for button color
        if (button != null && isSelected)
        {
            button.Select();
        }
    }
    
    /// <summary>
    /// Refresh current category display
    /// </summary>
    public void RefreshCategory(bool preserveScrollPosition = false)
    {
        if (offeringGenerator == null)
        {
            Debug.LogError("ShopOfferingGenerator not assigned!");
            return;
        }
        
        float targetVertical = 1f;
        float targetHorizontal = 0f;
        if (preserveScrollPosition && itemScrollRect != null)
        {
            targetVertical = Mathf.Clamp01(itemScrollRect.verticalNormalizedPosition);
            targetHorizontal = Mathf.Clamp01(itemScrollRect.horizontalNormalizedPosition);
        }

        // Clear existing tiles
        ClearTiles();
        
        // Get offerings for current category
        List<ShopOffering> offerings = offeringGenerator.GetOfferings(currentCategory);
        
        // Create tiles
        for (int i = 0; i < offerings.Count; i++)
        {
            CreateTile(offerings[i], i);
        }

        if (preserveScrollPosition)
        {
            SetScrollPosition(targetVertical, targetHorizontal);
        }
        else
        {
            SetScrollPosition(1f, 0f);
        }
    }

    private void SetScrollPosition(float verticalPosition, float horizontalPosition)
    {
        if (itemScrollRect == null)
            return;

        verticalPosition = Mathf.Clamp01(verticalPosition);
        horizontalPosition = Mathf.Clamp01(horizontalPosition);

        itemScrollRect.verticalNormalizedPosition = verticalPosition;
        itemScrollRect.horizontalNormalizedPosition = horizontalPosition;

        if (scrollResetRoutine != null)
        {
            StopCoroutine(scrollResetRoutine);
        }
        scrollResetRoutine = StartCoroutine(EnsureScrollPositionNextFrame(verticalPosition, horizontalPosition));
    }

    private void ResetCategoryScrollPosition()
    {
        if (categoryScrollRect == null)
            return;

        categoryScrollRect.verticalNormalizedPosition = 1f;
        categoryScrollRect.horizontalNormalizedPosition = 0f;
    }

    private IEnumerator EnsureScrollPositionNextFrame(float verticalPosition, float horizontalPosition)
    {
        yield return null;

        if (itemScrollRect != null)
        {
            itemScrollRect.verticalNormalizedPosition = verticalPosition;
            itemScrollRect.horizontalNormalizedPosition = horizontalPosition;
        }

        yield return new WaitForEndOfFrame();

        if (itemScrollRect != null)
        {
            itemScrollRect.verticalNormalizedPosition = verticalPosition;
            itemScrollRect.horizontalNormalizedPosition = horizontalPosition;
        }

        scrollResetRoutine = null;
    }
    
    /// <summary>
    /// Clear all current tiles
    /// </summary>
    private void ClearTiles()
    {
        foreach (ShopItemTile tile in currentTiles)
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }
        
        currentTiles.Clear();
    }
    
    /// <summary>
    /// Create a single item tile
    /// </summary>
    private void CreateTile(ShopOffering offering, int index)
    {
        if (itemTilePrefab == null || itemGridContainer == null)
        {
            Debug.LogError("Item tile prefab or grid container not assigned!");
            return;
        }
        
        GameObject tileObj = Instantiate(itemTilePrefab, itemGridContainer);
        ShopItemTile tile = tileObj.GetComponent<ShopItemTile>();
        
        if (tile != null)
        {
            // Get star icons from config
            Sprite filledStar = null;
            Sprite emptyStar = null;
            
            if (offeringGenerator.Config != null)
            {
                filledStar = offeringGenerator.Config.filledStarIcon;
                emptyStar = offeringGenerator.Config.emptyStarIcon;
            }
            
            // Setup tile
            tile.Setup(offering, currentCategory, index, filledStar, emptyStar, OnTileClicked);
            currentTiles.Add(tile);
        }
    }
    
    /// <summary>
    /// Handle tile click (show purchase confirmation)
    /// </summary>
    private void OnTileClicked(PartType partType, int index)
    {
        if (purchaseConfirmationUI != null)
        {
            purchaseConfirmationUI.ShowPurchaseConfirmation(partType, index, OnPurchaseComplete);
        }
        
        PlayButtonSound();
    }
    
    /// <summary>
    /// Called after successful purchase
    /// </summary>
    private void OnPurchaseComplete()
    {
        RefreshCategory(true);
    }
    
    /// <summary>
    /// Handle refresh button click
    /// </summary>
    private void OnRefreshClicked()
    {
        if (offeringGenerator != null)
        {
            // Refresh current category
            offeringGenerator.RefreshOfferings(currentCategory);
            RefreshCategory();
        }
        
        PlayButtonSound();
    }
    
    /// <summary>
    /// Update money display text
    /// </summary>
    private void UpdateMoneyDisplay(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"{amount}";
        }
    }
    
    /// <summary>
    /// Play button click sound
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// Check if shop is currently open
    /// </summary>
    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;
}

