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
    public Action<InteractionHandler> Callback;

    public InteractionOption(string id, string label, KeyCode key, InteractionOptionStyle style, bool isAvailable, Action<InteractionHandler> callback)
    {
        Id = id;
        Label = label;
        Key = key;
        Style = style;
        IsAvailable = isAvailable;
        Callback = callback;
    }

    public static InteractionOption Primary(string id, string label, KeyCode key, bool isAvailable, Action<InteractionHandler> callback)
    {
        return new InteractionOption(id, label, key, InteractionOptionStyle.Primary, isAvailable, callback);
    }

    public static InteractionOption Secondary(string id, string label, KeyCode key, bool isAvailable, Action<InteractionHandler> callback)
    {
        return new InteractionOption(id, label, key, InteractionOptionStyle.Secondary, isAvailable, callback);
    }
}

public interface IInteractionOptionsProvider
{
    void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options);
}
