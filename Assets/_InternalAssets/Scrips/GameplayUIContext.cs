using System.Collections.Generic;
using UnityEngine;

public class GameplayUIContext : MonoBehaviour
{
    private static GameplayUIContext instance;

    private readonly HashSet<object> hideRequests = new();
    private GameplayHUD registeredHud;

    public static GameplayUIContext Instance
    {
        get
        {
            if (instance == null)
            {
                Bootstrap();
            }

            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject host = new GameObject("GameplayUIContext");
        instance = host.AddComponent<GameplayUIContext>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterHud(GameplayHUD hud)
    {
        registeredHud = hud;
        UpdateHudVisibility();
    }

    public void UnregisterHud(GameplayHUD hud)
    {
        if (registeredHud == hud)
        {
            registeredHud = null;
        }
    }

    public void RequestHudHidden(object requester)
    {
        if (requester == null) return;
        if (hideRequests.Add(requester))
        {
            UpdateHudVisibility();
        }
    }

    public void ReleaseHud(object requester)
    {
        if (requester == null) return;
        if (hideRequests.Remove(requester))
        {
            UpdateHudVisibility();
        }
    }

    public bool IsHudHidden => hideRequests.Count > 0;

    private void UpdateHudVisibility()
    {
        if (registeredHud == null) return;

        bool shouldShow = hideRequests.Count == 0;
        registeredHud.SetVisible(shouldShow);
    }
}
