using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameplayHUD : MonoBehaviour
{
    public static GameplayHUD Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairRoot;

    [Header("Indicators")]
    [SerializeField] private GameObject moneyBackgroundRoot;
    [SerializeField] private TextMeshProUGUI moneyLabel;
    [SerializeField] private GameObject ammoBackgroundRoot;
    [SerializeField] private TextMeshProUGUI ammoCurrentLabel;
    [SerializeField] private TextMeshProUGUI ammoMaxLabel;
    [SerializeField] private string moneyFormat = "{0:n0}$";
    [SerializeField] private string ammoCurrentFormat = "{0}";
    [SerializeField] private string ammoMaxFormat = "{0}";
    [SerializeField] private string ammoUnavailableCurrentText = "--";
    [SerializeField] private string ammoUnavailableMaxText = "--";

    [Header("Interaction")]
    [SerializeField] private HUDInteractionPanel interactionPanel;

    private MoneySystem moneySystem;
    private bool hudVisible = true;
    private bool crosshairVisible = true;

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

        ClearAmmo();
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
        if (ammoBackgroundRoot != null)
        {
            ammoBackgroundRoot.SetActive(hasWeapon);
        }

        if (!hasWeapon)
        {
            ammoCurrentLabel.text = ammoUnavailableCurrentText;
            ammoMaxLabel.text = ammoUnavailableMaxText;
            return;
        }

        current = Mathf.Clamp(current, 0, max);
        ammoCurrentLabel.text = string.Format(ammoCurrentFormat, current);
        ammoMaxLabel.text = string.Format(ammoMaxFormat, max);
    }

    public void ClearAmmo()
    {
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
}
