using System;
using System.Collections.Generic;
using UnityEngine;

public enum InteractionOptionStyle
{
    Primary,
    Secondary
}

public struct InteractionOption
{
    public string Id;
    public string Label;
    public KeyCode Key;
    public InteractionOptionStyle Style;
    public bool IsAvailable;
    public bool RequiresHold;
    public Action<InteractionHandler> Callback;

    public InteractionOption(string id, string label, KeyCode key, InteractionOptionStyle style, bool isAvailable, bool requiresHold, Action<InteractionHandler> callback)
    {
        Id = id;
        Label = label;
        Key = key;
        Style = style;
        IsAvailable = isAvailable;
        RequiresHold = requiresHold;
        Callback = callback;
    }

    public static InteractionOption Primary(string id, string label, KeyCode key, bool isAvailable, Action<InteractionHandler> callback, bool requiresHold = false)
    {
        return new InteractionOption(id, label, key, InteractionOptionStyle.Primary, isAvailable, requiresHold, callback);
    }

    public static InteractionOption Secondary(string id, string label, KeyCode key, bool isAvailable, Action<InteractionHandler> callback, bool requiresHold = false)
    {
        return new InteractionOption(id, label, key, InteractionOptionStyle.Secondary, isAvailable, requiresHold, callback);
    }
}

public interface IInteractionOptionsProvider
{
    void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options);
}
