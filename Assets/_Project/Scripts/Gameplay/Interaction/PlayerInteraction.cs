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

    void InteractWithEnv()
    {
        Ray _ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(_ray, out RaycastHit _hit, envInteractionDistance + toolDistance, interactingLayer, QueryTriggerInteraction.Ignore))
        {
            InteractableObjects _curObj = _hit.collider.GetComponentInParent<InteractableObjects>();
            if (_curObj != null)
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
                UIController.instance.OnChangeInteraction(0, false);
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
