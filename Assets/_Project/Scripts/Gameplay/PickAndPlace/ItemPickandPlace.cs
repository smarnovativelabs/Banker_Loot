using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pick / place / throw core ported from the food game. SetObjectToCam parents the item to the held-item
/// point in front of the camera and lerps it there, ThrowPickedObjects releases it with a forward force and
/// OnItemRightPlaced lerps it onto a placement indicator. The food game's saving, category, rack and dynamic
/// placement code is not part of this port (this project has none of those systems yet).
/// </summary>
public class ItemPickandPlace : MonoBehaviour
{
    public string itemName;
    [Header("Pickable Items Positions/Scales/Rotation")]
    [Tooltip("Local position under the held-item point (child of the camera) while carried")]
    public Vector3 pickedPosition;
    public Vector3 pickedAngle;
    [Tooltip("Scale while carried")]
    public float camChildScale = 1f;
    [Tooltip("Throw force = camera forward * 100 * this (applied as a regular AddForce)")]
    public float throwForceMul = 1f;
    [Tooltip("Scale restored when the item is thrown or placed")]
    public float normalScale = 1f;
    public AudioClip itemPickSound;
    public AudioClip itemThrowSound;

    public bool isPlayerPicked = false;

    public enum ItemState
    {
        picking,
        placing,
        throwing,
        ready
    }
    public ItemState itemCondition = ItemState.ready;

    protected Transform defaultParent;
    protected int defaultLayer;
    protected Rigidbody body;
    Vector3 targetPosition;
    Quaternion targetRotation;
    Vector3 targetScale;
    float lerpTransition;
    UnityAction onCompleteLerpAction;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody>();
        defaultLayer = gameObject.layer;
    }

    protected virtual void Start()
    {
        defaultParent = transform.parent;
    }

    public virtual void Update()
    {
        if (itemCondition == ItemState.picking)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, lerpTransition);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, lerpTransition);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, lerpTransition);
            lerpTransition += Time.deltaTime * 2;
            if (lerpTransition > 1)
            {
                itemCondition = ItemState.ready;
                transform.localPosition = targetPosition;
                transform.localRotation = targetRotation;
                transform.localScale = targetScale;
                CompleteLerp();
            }
        }
        else if (itemCondition == ItemState.placing)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, lerpTransition);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpTransition);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, lerpTransition);
            lerpTransition += Time.deltaTime * 2;
            if (lerpTransition > 1)
            {
                itemCondition = ItemState.ready;
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                transform.localScale = targetScale;
                CompleteLerp();
            }
        }
        else if (itemCondition == ItemState.throwing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, lerpTransition);
            lerpTransition += Time.deltaTime * 50;
            if (lerpTransition > 1)
            {
                itemCondition = ItemState.ready;
                transform.localScale = targetScale;
                gameObject.layer = defaultLayer;
                CompleteLerp();
            }
        }
    }

    void CompleteLerp()
    {
        lerpTransition = 0;
        if (onCompleteLerpAction != null)
        {
            onCompleteLerpAction();
            onCompleteLerpAction = null;
        }
    }

    protected void SetColliders(bool _enable, bool _isTrigger = false)
    {
        var _colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = _enable;
            _colliders[i].isTrigger = _isTrigger;
        }
    }

    /// <summary>Makes the item a child of the camera's held-item point and moves it into the player's hands.</summary>
    public void SetObjectToCam()
    {
        var _controller = GameController.instance;
        if (_controller == null || _controller.parentOFPickedObj == null)
            return;

        BeginCarry(pickedPosition, pickedAngle, camChildScale);
        _controller.currentPicketItem = this;
        if (UIController.instance != null)
        {
            UIController.instance.OnChangeInteraction(1, true);
        }
        OnPickItem();
    }

    /// <summary>
    /// Core of picking: parents the item to the held-item point and lerps it to the given local pose.
    /// Used directly by systems that carry several items at once (NotePickupManager) and by SetObjectToCam.
    /// </summary>
    public void BeginCarry(Vector3 _localPosition, Vector3 _localAngle, float _scale)
    {
        var _controller = GameController.instance;
        if (_controller == null || _controller.parentOFPickedObj == null)
            return;

        gameObject.SetActive(true);
        transform.SetParent(_controller.parentOFPickedObj, true);
        SetColliders(false);
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        isPlayerPicked = true;
        lerpTransition = 0;
        itemCondition = ItemState.picking;
        targetPosition = _localPosition;
        targetRotation = Quaternion.Euler(_localAngle);
        targetScale = Vector3.one * _scale;
        if (itemPickSound != null && SoundController.instance != null)
        {
            SoundController.instance.OnPlayInteractionSound(itemPickSound);
        }
    }

    /// <summary>Releases the carried item with a forward force from the camera.</summary>
    public virtual void ThrowPickedObjects()
    {
        var _controller = GameController.instance;
        Vector3 _forward = _controller != null && _controller.gameMainCamera != null
            ? _controller.gameMainCamera.transform.forward
            : transform.forward;
        Release(_forward * 100f * throwForceMul);
        if (_controller != null)
        {
            _controller.currentPicketItem = null;
        }
        if (UIController.instance != null)
        {
            UIController.instance.OnChangeInteraction(0, false);
        }
    }

    /// <summary>Core of throwing: drops the item from the hands and adds the given force.</summary>
    public void Release(Vector3 _force)
    {
        transform.SetParent(defaultParent, true);
        SetColliders(true);
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.AddForce(_force);
        }
        isPlayerPicked = false;
        targetScale = Vector3.one * normalScale;
        lerpTransition = 0;
        itemCondition = ItemState.throwing;
        if (itemThrowSound != null && SoundController.instance != null)
        {
            SoundController.instance.OnPlayInteractionSound(itemThrowSound);
        }
    }

    /// <summary>Moves the carried item onto a placement indicator and drops it from the player's hands.</summary>
    public virtual void PlaceItem(Transform _indicator)
    {
        transform.SetParent(defaultParent, true);
        SetColliders(true);
        if (body != null)
        {
            body.isKinematic = true;
        }
        isPlayerPicked = false;
        if (GameController.instance != null)
        {
            GameController.instance.currentPicketItem = null;
        }
        if (UIController.instance != null)
        {
            UIController.instance.OnChangeInteraction(0, false);
        }
        OnItemRightPlaced(_indicator);
    }

    public void OnItemRightPlaced(Transform _placingIndicaterRef)
    {
        targetPosition = _placingIndicaterRef.position;
        targetRotation = _placingIndicaterRef.rotation;
        targetScale = _placingIndicaterRef.lossyScale;
        lerpTransition = 0;
        itemCondition = ItemState.placing;
    }

    public virtual void OnPickItem()
    {
    }

    public virtual bool CanPickItem()
    {
        return GameController.instance != null && !GameController.instance.IsHoldingItem;
    }
}
