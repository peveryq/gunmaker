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
    [SerializeField] private TextMeshProUGUI moneyLabel;
    [SerializeField] private TextMeshProUGUI ammoLabel;
    [SerializeField] private string moneyFormat = "{0:n0}$";
    [SerializeField] private string ammoFormat = "{0}/{1}";
    [SerializeField] private string ammoUnavailableText = "--/--";

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
    }

    private void OnDisable()
    {
        DisconnectMoneySystem();

        if (GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.UnregisterHud(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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

        if (root != null)
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
    }

    public void SetAmmo(int current, int max)
    {
        if (ammoLabel == null) return;

        if (max <= 0)
        {
            ammoLabel.text = ammoUnavailableText;
            return;
        }

        current = Mathf.Clamp(current, 0, max);
        ammoLabel.text = string.Format(ammoFormat, current, max);
    }

    public void ClearAmmo()
    {
        if (ammoLabel == null) return;
        ammoLabel.text = ammoUnavailableText;
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
