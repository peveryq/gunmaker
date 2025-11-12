using System.Collections.Generic;
using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private float maxInteractionDistance = 5f;
    [SerializeField] private float aimAssistRadius = 0f;
    [SerializeField] private LayerMask interactableLayer = -1;

    [Header("Drop Settings")]
    [Tooltip("Position where items are dropped (relative to camera)")]
    [SerializeField] private Vector3 dropPosition = new Vector3(0f, -0.5f, 1.5f);
    [SerializeField] private float dropForce = 5f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform itemHoldPoint;

    private IInteractable currentTarget;
    private ItemPickup currentItem;
    private WeaponController currentWeaponController;

    private readonly List<InteractionOption> optionsBuffer = new();
    private readonly List<InteractionOption> activeOptions = new();

    private GameplayHUD gameplayHud;

    private void Start()
    {
        if (playerCamera == null)
        {
            FirstPersonController fpsController = GetComponent<FirstPersonController>();
            if (fpsController != null)
            {
                playerCamera = fpsController.PlayerCamera;
            }
            else
            {
                playerCamera = Camera.main;
            }
        }

        if (itemHoldPoint == null && playerCamera != null)
        {
            GameObject holdPointObj = new GameObject("ItemHoldPoint");
            holdPointObj.transform.SetParent(playerCamera.transform);
            holdPointObj.transform.localPosition = Vector3.zero;
            holdPointObj.transform.localRotation = Quaternion.identity;
            itemHoldPoint = holdPointObj.transform;
        }

        BindGameplayHud();
    }

    private void Update()
    {
        DetectInteractable();
        ProcessInteractionInputs();

        if (Input.GetKeyDown(dropKey) && currentItem != null)
        {
            DropCurrentItem();
        }
    }

    private void LateUpdate()
    {
        if (gameplayHud == null)
        {
            BindGameplayHud();
        }
    }

    private void BindGameplayHud()
    {
        gameplayHud = GameplayHUD.Instance ?? FindFirstObjectByType<GameplayHUD>();
        if (gameplayHud != null)
        {
            gameplayHud.InteractionPanel?.BindHandler(this);
            UpdateHudAmmo();
        }
    }

    private void DetectInteractable()
    {
        IInteractable previousTarget = currentTarget;
        Workbench previousWorkbench = previousTarget as Workbench;

        currentTarget = null;
        bool canInteractWithTarget = false;

        if (playerCamera == null)
        {
            UpdateInteractionOptions(null);
            return;
        }

        IInteractable detected = FindInteractable();
        if (detected != null)
        {
            float distance = Vector3.Distance(transform.position, detected.Transform.position);
            if (distance <= detected.InteractionRange)
            {
                currentTarget = detected;
                canInteractWithTarget = detected.CanInteract(this);
            }
        }

        if (previousTarget != null && previousTarget != currentTarget)
        {
            DisableOutline(previousTarget);
            previousWorkbench?.HidePreview();
        }

        if (currentTarget != null)
        {
            if (currentTarget.ShowOutline)
            {
                EnableOutline(currentTarget);
            }

            if (currentTarget is Workbench currentWorkbench)
            {
                if (canInteractWithTarget)
                {
                    currentWorkbench.ShowPreview();
                }
                else
                {
                    currentWorkbench.HidePreview();
                }
            }
        }

        UpdateInteractionOptions(currentTarget);
    }

    private IInteractable FindInteractable()
    {
        IInteractable detected = null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxInteractionDistance, interactableLayer);

        if (hits.Length > 1)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(playerCamera.transform))
            {
                continue;
            }

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                detected = interactable;
                break;
            }
        }

        if (detected == null && aimAssistRadius > 0f)
        {
            Vector3 checkPoint = ray.GetPoint(Mathf.Min(maxInteractionDistance * 0.5f, 3f));
            Collider[] nearbyColliders = Physics.OverlapSphere(checkPoint, aimAssistRadius, interactableLayer);
            float closestDistance = float.MaxValue;

            foreach (Collider col in nearbyColliders)
            {
                if (col.transform.IsChildOf(transform) || col.transform.IsChildOf(playerCamera.transform))
                {
                    continue;
                }

                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = col.GetComponentInParent<IInteractable>();
                }

                if (interactable != null)
                {
                    float dist = Vector3.Distance(interactable.Transform.position, checkPoint);
                    if (dist < closestDistance)
                    {
                        Vector3 toTarget = interactable.Transform.position - playerCamera.transform.position;
                        float dotProduct = Vector3.Dot(playerCamera.transform.forward, toTarget.normalized);
                        if (dotProduct > 0.7f)
                        {
                            detected = interactable;
                            closestDistance = dist;
                        }
                    }
                }
            }
        }

        return detected;
    }

    private void UpdateInteractionOptions(IInteractable target)
    {
        optionsBuffer.Clear();

        if (target != null)
        {
            IInteractionOptionsProvider provider = GetOptionsProvider(target);
            if (provider != null)
            {
                provider.PopulateInteractionOptions(this, optionsBuffer);
            }

            if (optionsBuffer.Count == 0)
            {
                string prompt = target.GetInteractionPrompt(this);
                if (!string.IsNullOrEmpty(prompt))
                {
                    string label = NormalizePrompt(prompt);
                    bool available = target.CanInteract(this);
                    optionsBuffer.Add(InteractionOption.Primary("default", label, interactKey, available, handler => handler.PerformInteraction(target)));
                }
            }
        }

        activeOptions.Clear();

        if (optionsBuffer.Count == 0)
        {
            gameplayHud?.InteractionPanel?.Hide();
            return;
        }

        activeOptions.AddRange(optionsBuffer);
        if (gameplayHud != null && gameplayHud.InteractionPanel != null)
        {
            gameplayHud.InteractionPanel.BindHandler(this);
            gameplayHud.InteractionPanel.ShowOptions(activeOptions);
        }
    }

    private static IInteractionOptionsProvider GetOptionsProvider(IInteractable target)
    {
        if (target is MonoBehaviour behaviour)
        {
            return behaviour.GetComponent<IInteractionOptionsProvider>();
        }

        return null;
    }

    private static string NormalizePrompt(string prompt)
    {
        if (string.IsNullOrEmpty(prompt)) return string.Empty;

        string trimmed = prompt.Trim();
        if (trimmed.StartsWith("["))
        {
            int closingBracket = trimmed.IndexOf(']');
            if (closingBracket >= 0 && closingBracket < trimmed.Length - 1)
            {
                trimmed = trimmed.Substring(closingBracket + 1).Trim();
            }
        }

        return trimmed;
    }

    private void ProcessInteractionInputs()
    {
        bool handled = false;

        for (int i = 0; i < activeOptions.Count; i++)
        {
            InteractionOption option = activeOptions[i];
            if (!option.IsAvailable || option.Key == KeyCode.None)
            {
                continue;
            }

            if (Input.GetKeyDown(option.Key))
            {
                option.Callback?.Invoke(this);
                handled = true;
                break;
            }
        }

        if (!handled && Input.GetKeyDown(interactKey) && currentTarget != null && activeOptions.Count == 0)
        {
            PerformInteraction(currentTarget);
        }
    }

    private void EnableOutline(IInteractable interactable)
    {
        if (interactable is MonoBehaviour mb)
        {
            MonoBehaviour outlinable = mb.GetComponent("Outlinable") as MonoBehaviour;
            if (outlinable != null)
            {
                outlinable.enabled = true;
            }
        }
    }

    private void DisableOutline(IInteractable interactable)
    {
        if (interactable is MonoBehaviour mb)
        {
            MonoBehaviour outlinable = mb.GetComponent("Outlinable") as MonoBehaviour;
            if (outlinable != null)
            {
                outlinable.enabled = false;
            }
        }
    }

    public void PerformInteraction(IInteractable target)
    {
        if (target == null) return;
        if (!target.CanInteract(this)) return;
        if (target.Interact(this))
        {
            // interaction may change state; new options will be resolved next frame
        }
    }

    // Item management (for ItemPickup compatibility)
    public bool PickupItem(ItemPickup item)
    {
        if (item == null || itemHoldPoint == null) return false;

        if (currentItem != null)
        {
            DropCurrentItem();
        }

        item.Pickup(itemHoldPoint);
        currentItem = item;

        WeaponController weapon = item.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.Equip(playerCamera);
            AttachWeaponController(weapon);
        }
        else
        {
            DetachWeaponController();
        }

        return true;
    }

    public void DropCurrentItem()
    {
        if (currentItem == null || playerCamera == null) return;

        WeaponController weapon = currentItem.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.Unequip();
        }

        Vector3 worldDropPosition = playerCamera.transform.position +
                                    playerCamera.transform.right * dropPosition.x +
                                    playerCamera.transform.up * dropPosition.y +
                                    playerCamera.transform.forward * dropPosition.z;

        Quaternion baseRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(currentItem.DropRotation);

        Vector3 dropForceVector = playerCamera.transform.forward * dropForce;

        currentItem.Drop(worldDropPosition, dropForceVector, finalRotation);
        currentItem = null;
        DetachWeaponController();
    }

    public void ClearCurrentItem()
    {
        currentItem = null;
        DetachWeaponController();
    }

    public void ForcePickupItem(ItemPickup item)
    {
        PickupItem(item);
    }

    private void AttachWeaponController(WeaponController weapon)
    {
        if (weapon == null)
        {
            return;
        }

        DetachWeaponController();
        currentWeaponController = weapon;
        currentWeaponController.AmmoChanged += HandleAmmoChanged;
        HandleAmmoChanged(currentWeaponController.CurrentAmmo, currentWeaponController.MaxAmmo);
    }

    private void DetachWeaponController()
    {
        if (currentWeaponController != null)
        {
            currentWeaponController.AmmoChanged -= HandleAmmoChanged;
            currentWeaponController = null;
        }

        gameplayHud?.ClearAmmo();
    }

    private void HandleAmmoChanged(int current, int max)
    {
        gameplayHud?.SetAmmo(current, max);
    }

    private void UpdateHudAmmo()
    {
        if (currentWeaponController != null)
        {
            HandleAmmoChanged(currentWeaponController.CurrentAmmo, currentWeaponController.MaxAmmo);
        }
        else
        {
            gameplayHud?.ClearAmmo();
        }
    }

    // Properties
    public bool IsHoldingItem => currentItem != null;
    public ItemPickup CurrentItem => currentItem;
    public Camera PlayerCamera => playerCamera;
    public Transform ItemHoldPoint => itemHoldPoint;
    public IInteractable CurrentTarget => currentTarget;
    public KeyCode InteractKey => interactKey;
    public KeyCode DropKey => dropKey;
}

