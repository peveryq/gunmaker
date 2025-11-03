using UnityEngine;

/// <summary>
/// Interface for all interactable objects (items, workbench, doors, etc.)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when player interacts with this object (presses E)
    /// </summary>
    /// <param name="player">Player's ItemHandler reference</param>
    /// <returns>True if interaction was successful</returns>
    bool Interact(InteractionHandler player);
    
    /// <summary>
    /// Check if this object can be interacted with right now
    /// </summary>
    /// <param name="player">Player's ItemHandler reference</param>
    /// <returns>True if interaction is possible</returns>
    bool CanInteract(InteractionHandler player);
    
    /// <summary>
    /// Get interaction prompt text to show player
    /// </summary>
    string GetInteractionPrompt(InteractionHandler player);
    
    /// <summary>
    /// Transform of the interactable object
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// Maximum distance for interaction
    /// </summary>
    float InteractionRange { get; }
    
    /// <summary>
    /// Should this object show outline when looked at?
    /// </summary>
    bool ShowOutline { get; }
}

