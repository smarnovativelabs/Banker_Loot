using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sits on the full screen touchpad. A quick tap presses the interact button, a long swipe
/// completes the "look around" tutorial step.
/// </summary>
public class EnvInteract : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static EnvInteract instance;
    bool firstSwiped = false;
    Vector3 m_StartPos;
    Vector3 deltaPosition;
    Vector3 currentPosition;
    float tapTimer = 0f;
    int tapPointerId = -1;
    float swipeTimer = 0f;

    private void Awake()
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

    public void OnPointerDown(PointerEventData data)
    {
        if (tapPointerId < 0)
        {
            tapPointerId = data.pointerId;
            m_StartPos = data.position;
            deltaPosition = data.position;
            currentPosition = data.position;
        }
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (data.pointerId == tapPointerId)
        {
            tapPointerId = -1;

            if (tapTimer < 0.5f)
            {
                float distance = Vector3.Distance(m_StartPos, data.position);
                if (distance < 0.25f && UIController.instance != null)
                {
                    UIController.instance.OnInteractBtnPressed();
                }
            }
            tapTimer = 0f;
        }
    }

    public void OnInGameAd()
    {
        tapPointerId = -1;
        tapTimer = 0;
    }

    void Update()
    {
        if (tapPointerId >= 0)
        {
            tapTimer += Time.deltaTime;
            if (!firstSwiped && Vector3.Distance(currentPosition, deltaPosition) > 0.05f)
            {
                swipeTimer += Time.deltaTime;
                if (swipeTimer >= 1f)
                {
                    firstSwiped = true;
                    swipeTimer = 0f;
                    CompleteTutorialStep("Controls_T0_Swipe");
                }
            }
            deltaPosition = currentPosition;
        }
#if UNITY_EDITOR
        // editor shortcuts to skip the controls tutorial steps
        if (Input.GetKeyDown(KeyCode.J))
        {
            CompleteTutorialStep("Controls_T0_Move");
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            CompleteTutorialStep("Controls_T0_Swipe");
        }
#endif
    }

    static void CompleteTutorialStep(string _subTaskId)
    {
        if (GameTutorialManager.instance != null)
        {
            GameTutorialManager.instance.OnSubTaskComplete("Control_T0", _subTaskId);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        currentPosition = eventData.position;
    }
}
