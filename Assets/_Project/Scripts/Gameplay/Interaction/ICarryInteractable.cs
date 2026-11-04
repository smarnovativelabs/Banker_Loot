/// <summary>
/// An interactable that can still be aimed at and used while the player carries notes (the washing machine, the cash
/// counting machine...). The currency button forwards its press to OnInteract instead of throwing a note.
/// </summary>
public interface ICarryInteractable : InteractableObjects
{
}
