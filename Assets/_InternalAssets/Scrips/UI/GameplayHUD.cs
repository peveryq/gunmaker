using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHUD : MonoBehaviour
{
    public static GameplayHUD Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairRoot;
    [SerializeField] private CrosshairController crosshairController;

    [Header("Indicators")]
    [SerializeField] private GameObject moneyBackgroundRoot;
    [SerializeField] private TextMeshProUGUI moneyLabel;
    [SerializeField] private GameObject ammoBackgroundRoot;
    [SerializeField] private TextMeshProUGUI ammoCurrentLabel;
    [SerializeField] private TextMeshProUGUI ammoMaxLabel;
    [SerializeField] private GameObject ammoIconRoot;
    [SerializeField] private string moneyFormat = "{0:n0}$";
    [SerializeField] private string ammoCurrentFormat = "{0}";
    [SerializeField] private string ammoMaxFormat = "{0}";
    [SerializeField] private string ammoUnavailableCurrentText = "--";
    [SerializeField] private string ammoUnavailableMaxText = "--";

    [Header("Reload Indicator")]
    [SerializeField] private GameObject reloadRoot;
    [SerializeField] private Image reloadFillImage;
    [SerializeField] private Image reloadSpinnerImage;
    [SerializeField] private float reloadSpinnerSpeed = 180f;

    [Header("Unwelded Barrel Warning")]
    [SerializeField] private GameObject unweldedBarrelWarningRoot;
    [SerializeField] private TextMeshProUGUI unweldedBarrelWarningText;
    [SerializeField] private Image unweldedBarrelWarningIcon;
    [SerializeField] private string unweldedBarrelWarningTextFormat = "Barrel not welded";

    [Header("Interaction")]
    [SerializeField] private HUDInteractionPanel interactionPanel;

    private MoneySystem moneySystem;
    private bool hudVisible = true;
    private bool crosshairVisible = true;
    private bool reloadIndicatorVisible;
    private Quaternion reloadSpinnerInitialRotation;
    private bool reloadSpinnerInitialized;
    private bool hasWeaponEquipped;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
        {
            root = gameObject;
        }

        if (interactionPanel != null)
        {
            interactionPanel.Hide();
        }

        CacheReloadSpinnerRotation();
        SetReloadState(false);
        ClearAmmo();
        SetUnweldedBarrelWarning(false);
    }

    private void OnEnable()
    {
        TryConnectMoneySystem();

        GameplayUIContext.Instance.RegisterHud(this);
    }

    private void OnDisable()
    {
        DisconnectMoneySystem();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.UnregisterHud(this);
        }
    }

    private void Update()
    {
        if (moneySystem == null)
        {
            TryConnectMoneySystem();
        }

        if (reloadIndicatorVisible && reloadSpinnerImage != null && !Mathf.Approximately(reloadSpinnerSpeed, 0f))
        {
            float delta = Time.deltaTime;
            if (delta <= 0f)
            {
                delta = Time.unscaledDeltaTime;
            }

            reloadSpinnerImage.rectTransform.Rotate(0f, 0f, -reloadSpinnerSpeed * delta);
        }
    }

    public HUDInteractionPanel InteractionPanel => interactionPanel;

    public void SetVisible(bool visible)
    {
        hudVisible = visible;

        if (root != null && root != gameObject)
        {
            root.SetActive(visible);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (interactionPanel != null && !visible)
        {
            interactionPanel.Hide();
        }

        if (crosshairRoot != null)
        {
            crosshairRoot.SetActive(visible && crosshairVisible);
        }
    }

    public void SetCrosshairVisible(bool visible)
    {
        crosshairVisible = visible;
        if (crosshairRoot != null)
        {
            crosshairRoot.SetActive(hudVisible && crosshairVisible);
        }
    }

    public void SetMoney(int amount)
    {
        if (moneyLabel == null) return;

        moneyLabel.text = string.Format(moneyFormat, Mathf.Max(0, amount));

        if (moneyBackgroundRoot != null && !moneyBackgroundRoot.activeSelf)
        {
            moneyBackgroundRoot.SetActive(true);
        }
    }

    public void SetAmmo(int current, int max)
    {
        if (ammoCurrentLabel == null || ammoMaxLabel == null)
        {
            return;
        }

        bool hasWeapon = max > 0;
        hasWeaponEquipped = hasWeapon;
        
        if (crosshairController != null)
        {
            crosshairController.SetWeaponEquipped(hasWeapon);
        }
        
        if (ammoBackgroundRoot != null)
        {
            ammoBackgroundRoot.SetActive(hasWeapon);
        }

        if (!hasWeapon)
        {
            ammoCurrentLabel.text = ammoUnavailableCurrentText;
            ammoMaxLabel.text = ammoUnavailableMaxText;
            SetReloadState(false);
            return;
        }

        current = Mathf.Clamp(current, 0, max);
        ammoCurrentLabel.text = string.Format(ammoCurrentFormat, current);
        ammoMaxLabel.text = string.Format(ammoMaxFormat, max);
        UpdateAmmoIconVisibility();
    }

    public void ClearAmmo()
    {
        hasWeaponEquipped = false;
        
        if (crosshairController != null)
        {
            crosshairController.SetWeaponEquipped(false);
        }
        
        if (ammoCurrentLabel != null)
        {
            ammoCurrentLabel.text = ammoUnavailableCurrentText;
        }

        if (ammoMaxLabel != null)
        {
            ammoMaxLabel.text = ammoUnavailableMaxText;
        }

        if (ammoBackgroundRoot != null)
        {
            ammoBackgroundRoot.SetActive(false);
        }

        SetReloadState(false);
    }
    
    /// <summary>
    /// Called when shooting starts. Moves weapon lines away from center.
    /// </summary>
    public void StartCrosshairShooting()
    {
        if (crosshairController != null)
        {
            crosshairController.StartShooting();
        }
    }

    /// <summary>
    /// Called when shooting stops. Returns weapon lines to original position.
    /// </summary>
    public void StopCrosshairShooting()
    {
        if (crosshairController != null)
        {
            crosshairController.StopShooting();
        }
    }
    
    /// <summary>
    /// Shows hit lines on crosshair based on hit zone. Called when a target is hit.
    /// </summary>
    public void ShowHitLines(CrosshairController.HitZone zone)
    {
        if (crosshairController != null)
        {
            crosshairController.ShowHitLines(zone);
        }
    }

    /// <summary>
    /// Shows kill lines on crosshair. Called when a target is killed (HP reaches 0).
    /// </summary>
    public void ShowKillLines()
    {
        if (crosshairController != null)
        {
            crosshairController.ShowKillLines();
        }
    }

    /// <summary>
    /// Sets aiming state. Hides weapon lines when aiming.
    /// </summary>
    public void SetAiming(bool aiming)
    {
        if (crosshairController != null)
        {
            crosshairController.SetAiming(aiming);
        }
    }

    public void SetReloadState(bool active)
    {
        CacheReloadSpinnerRotation();

        reloadIndicatorVisible = active;
        UpdateAmmoIconVisibility();

        if (reloadRoot != null && reloadRoot.activeSelf != active)
        {
            reloadRoot.SetActive(active);
        }

        if (!active)
        {
            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = 0f;
            }

            ResetReloadSpinnerRotation();
        }
    }

    public void SetReloadProgress(float progress)
    {
        if (reloadFillImage == null)
        {
            return;
        }

        reloadFillImage.fillAmount = Mathf.Clamp01(progress);
    }

    private void UpdateAmmoIconVisibility()
    {
        bool showBulletIcon = hasWeaponEquipped && !reloadIndicatorVisible;

        if (ammoIconRoot != null)
        {
            ammoIconRoot.SetActive(showBulletIcon);
        }

        if (reloadSpinnerImage != null)
        {
            reloadSpinnerImage.gameObject.SetActive(reloadIndicatorVisible);
        }
    }

    private void CacheReloadSpinnerRotation()
    {
        if (reloadSpinnerImage != null && !reloadSpinnerInitialized)
        {
            reloadSpinnerInitialRotation = reloadSpinnerImage.rectTransform.localRotation;
            reloadSpinnerInitialized = true;
        }
    }

    private void ResetReloadSpinnerRotation()
    {
        if (reloadSpinnerImage != null && reloadSpinnerInitialized)
        {
            reloadSpinnerImage.rectTransform.localRotation = reloadSpinnerInitialRotation;
        }
    }

    private void TryConnectMoneySystem()
    {
        if (moneySystem != null) return;

        moneySystem = MoneySystem.Instance ?? FindFirstObjectByType<MoneySystem>();
        if (moneySystem != null)
        {
            moneySystem.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(moneySystem.CurrentMoney);
        }
    }

    private void DisconnectMoneySystem()
    {
        if (moneySystem != null)
        {
            moneySystem.OnMoneyChanged -= HandleMoneyChanged;
            moneySystem = null;
        }
    }

    private void HandleMoneyChanged(int amount)
    {
        SetMoney(amount);
    }

    /// <summary>
    /// Shows or hides the unwelded barrel warning
    /// </summary>
    public void SetUnweldedBarrelWarning(bool show)
    {
        if (unweldedBarrelWarningRoot != null)
        {
            unweldedBarrelWarningRoot.SetActive(show);
        }

        if (show && unweldedBarrelWarningText != null && !string.IsNullOrEmpty(unweldedBarrelWarningTextFormat))
        {
            unweldedBarrelWarningText.text = unweldedBarrelWarningTextFormat;
        }
    }
}
