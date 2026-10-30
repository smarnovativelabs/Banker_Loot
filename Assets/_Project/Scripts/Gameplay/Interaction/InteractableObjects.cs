using UnityEngine;

/// <summary>
/// Implement on any object the player can look at and press the interact button on.
/// PlayerInteraction raycasts from the camera, calls OnHoverItems while the object is targeted,
/// TurnOffOutline when it is no longer targeted and OnInteract when the interact button / a screen tap is pressed.
/// </summary>
public interface InteractableObjects
{
    public abstract void OnHoverItems();
    public abstract void OnInteract();
    public abstract void TurnOffOutline();
}
