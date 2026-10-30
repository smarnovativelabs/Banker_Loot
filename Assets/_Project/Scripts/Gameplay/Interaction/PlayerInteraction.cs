using UnityEngine;

/// <summary>
/// Lives on the player camera. Raycasts forward every frame and keeps track of the interactable
/// the player is looking at (same pattern as the food game).
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction instance;
    public float envInteractionDistance = 4f;
    public float toolDistance = 0f;
    public LayerMask interactingLayer = ~0;
    public InteractableObjects currentInteractingObj;
    public GameObject interactingObj;
    bool gameStarted = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame(bool _start)
    {
        gameStarted = _start;
    }

    void Update()
    {
        if (!gameStarted)
            return;
        InteractWithEnv();
    }

    /// <summary>A single carried item (not a pile of notes).</summary>
    static bool IsHoldingItem()
    {
        return GameController.instance != null && GameController.instance.currentPicketItem != null;
    }

    static bool IsCarryingNotes()
    {
        return NotePickupManager.instance != null && NotePickupManager.instance.HeldCount > 0;
    }

    /// <summary>
    /// While the hands are busy only notes stay targetable: carrying notes blocks every other interaction, and
    /// carrying another item still lets the player aim at a note so the currency button can explain why it fails.
    /// </summary>
    static bool CanTarget(InteractableObjects _obj)
    {
        if (IsHoldingItem() || IsCarryingNotes())
            return _obj is PickableNote;
        return true;
    }

    void InteractWithEnv()
    {
        Ray _ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(_ray, out RaycastHit _hit, envInteractionDistance + toolDistance, interactingLayer, QueryTriggerInteraction.Ignore))
        {
            InteractableObjects _curObj = _hit.collider.GetComponentInParent<InteractableObjects>();
            if (_curObj != null && CanTarget(_curObj))
            {
                if (currentInteractingObj != null && _curObj != currentInteractingObj)
                {
                    currentInteractingObj.TurnOffOutline();
                }
                currentInteractingObj = _curObj;
                interactingObj = (currentInteractingObj as MonoBehaviour).gameObject;
                _curObj.OnHoverItems();
                return;
            }
        }
        ClearTarget();
    }

    void ClearTarget()
    {
        if (currentInteractingObj != null)
        {
            currentInteractingObj.TurnOffOutline();
            if (UIController.instance != null)
            {
                UIController.instance.DisplayHoverObjectName("", false);
                // a carried item keeps its throw button, everything else hides the generic button
                UIController.instance.OnChangeInteraction(IsHoldingItem() ? 1 : 0, IsHoldingItem());
            }
        }
        currentInteractingObj = null;
        interactingObj = null;
    }

    public void SetToolDistance(float _val)
    {
        toolDistance = _val;
    }

    /// <summary>Called by the interact button / screen tap.</summary>
    public void OnInteract()
    {
        if (IsHoldingItem())
        {
            GameController.instance.currentPicketItem.ThrowPickedObjects();
            return;
        }
        if (currentInteractingObj != null)
        {
            currentInteractingObj.OnInteract();
        }
    }

    public bool isAlreadyIntreact()
    {
        return currentInteractingObj != null;
    }

    public void UpdateCurrentInteractingObj()
    {
        currentInteractingObj = null;
    }
}
