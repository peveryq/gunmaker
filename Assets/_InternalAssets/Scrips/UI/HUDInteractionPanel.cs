using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDInteractionPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform container;
    [SerializeField] private InteractionButtonView buttonPrefab;

    private readonly List<InteractionButtonView> activeButtons = new();
    private readonly List<InteractionButtonView> pooledButtons = new();
    private readonly List<InteractionOption> currentOptions = new();

    private InteractionHandler boundHandler;

    private RectTransform ContainerRectTransform => (container ?? transform) as RectTransform;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        Hide();
    }

    public void BindHandler(InteractionHandler handler)
    {
        boundHandler = handler;
    }

    public void ShowOptions(IReadOnlyList<InteractionOption> options)
    {
        currentOptions.Clear();

        if (options == null || options.Count == 0)
        {
            Hide();
            return;
        }

        // Add all options (including unavailable ones - they will be shown as disabled)
        for (int i = 0; i < options.Count; i++)
        {
            currentOptions.Add(options[i]);
        }

        if (currentOptions.Count == 0)
        {
            Hide();
            return;
        }

        EnsureButtonCount(currentOptions.Count);

        for (int i = 0; i < currentOptions.Count; i++)
        {
            InteractionOption option = currentOptions[i];
            InteractionButtonView view = activeButtons[i];
            view.gameObject.SetActive(true);
            view.transform.SetSiblingIndex(i);
            view.Configure(option, boundHandler);
        }

        if (root != null && !root.activeSelf)
        {
            root.SetActive(true);
        }

        ForceContainerLayoutUpdate();
    }

    public void Hide()
    {
        currentOptions.Clear();

        for (int i = 0; i < activeButtons.Count; i++)
        {
            InteractionButtonView view = activeButtons[i];
            view.gameObject.SetActive(false);
            pooledButtons.Add(view);
        }

        activeButtons.Clear();

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public IReadOnlyList<InteractionOption> CurrentOptions => currentOptions;

    private void EnsureButtonCount(int requiredCount)
    {
        while (activeButtons.Count < requiredCount)
        {
            InteractionButtonView view;
            if (pooledButtons.Count > 0)
            {
                int index = pooledButtons.Count - 1;
                view = pooledButtons[index];
                pooledButtons.RemoveAt(index);
            }
            else
            {
                view = Instantiate(buttonPrefab, container != null ? container : transform);
            }

            Transform parentTransform = container != null ? container : transform;
            if (view.transform.parent != parentTransform)
            {
                view.transform.SetParent(parentTransform, false);
            }

            view.transform.SetAsLastSibling();
            view.gameObject.SetActive(true);
            activeButtons.Add(view);
        }

        for (int i = activeButtons.Count - 1; i >= requiredCount; i--)
        {
            InteractionButtonView view = activeButtons[i];
            view.gameObject.SetActive(false);
            pooledButtons.Add(view);
            activeButtons.RemoveAt(i);
        }
    }

    private void ForceContainerLayoutUpdate()
    {
        RectTransform rectTransform = ContainerRectTransform;
        if (rectTransform != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            Canvas.ForceUpdateCanvases();
        }
    }
}
