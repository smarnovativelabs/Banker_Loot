using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;

public class GameTutorialManager : MonoBehaviour
{
    [Header("Tutorial Panel Fields")]
    public Transform handAnimDefaultParent;
    public GameObject tutorialOverlayPanel;
    public GameObject overlayTapPanel;
    public RectTransform cutOutObject;
    public RectTransform animHandObj;
    public RectTransform overlayInstructionCntnr;
    public Text overlayInstructionTxt;
    public Text overlayInstructionTapPress;
    public Transform cutOutObjectParent;

    public GameObject targetPointer;
    public GameObject targetArrow;
    public NavmeshPathDraw navmeshPathDraw;
    public GameObject spawnEffect; /// jo effect spawn hoga uska reference idhr rakhna
    public AreaTutorials[] tutorialAreas;
    public static GameTutorialManager instance;

    #region Game Integration Hooks
    // Wire these from the game's own UI / objective systems (all optional, the tutorial runs without them).
    /// <summary>instruction, heading, show, displayOnTop, instantDisplay</summary>
    public static event Action<string, string, bool, bool, bool> QuestInstructionRequested;
    public static event Action QuestInstructionRemoved;
    /// <summary>Raised when the game should show its normal objective (tutorial finished or area is not high priority).</summary>
    public static event Action ObjectiveRequested;
    public static event Action TutorialCompleted;
    /// <summary>Return true while a regular objective is on screen (lets low priority tutorials wait).</summary>
    public static Func<bool> IsObjectiveActive;
    /// <summary>Resolves "Store_<ids>" style dynamic subtask button ids to the UI object to cut out.</summary>
    public static Func<List<string>, GameObject> DynamicButtonResolver;
    /// <summary>Resolves "Box_<ids>" / "Item_<ids>" style dynamic subtask ids (first arg: Box or Item) to a world target.</summary>
    public static Func<string, List<string>, Transform> TargetResolver;
    #endregion
   
    public AreaTutorials currentArea;
    bool isDataInitialized;
    public bool isTutorialComplete = false;
    private const string DataResetKey = "DataResetDone";
    private void Awake()
    {
        instance = this;
    }

    #region Initialization
    public IEnumerator InitializeTutorial()
    {
        // ✅ STEP 3: Set up new tutorial
        tutorialOverlayPanel.SetActive(false);
        cutOutObject.gameObject.SetActive(false);
        overlayInstructionCntnr.gameObject.SetActive(false);
        targetPointer.SetActive(false);
        cutOutObjectParent.gameObject.SetActive(true);
        animHandObj.gameObject.SetActive(false);
        overlayTapPanel.SetActive(false);
        GameController.instance.AddSavingAction(SaveTutorialData);

        TutorialSavable _savedTutorial = (TutorialSavable)SerializationManager.LoadFile("TutorialData");

        if (_savedTutorial != null)
        {
            for (int i = 0; i < tutorialAreas.Length; i++)
            {
                if (i < _savedTutorial.areas.Count)
                {
                    tutorialAreas[i].areaTutorialComplete = _savedTutorial.areas[i].isTutorialCompleted;
                    tutorialAreas[i].canShowAreaTutorial = tutorialAreas[i].isActionDependentTutorial
                        ? _savedTutorial.areas[i].canShowAreaTutorial
                        : true;

                    foreach (string taskId in _savedTutorial.areas[i].taskIds)
                    {
                        int taskIndex = tutorialAreas[i].tutorialTasks.FindIndex(t => t.taskId == taskId);
                        if (taskIndex >= 0)
                        {
                            tutorialAreas[i].tutorialTasks[taskIndex].taskComplete = true;
                        }
                    }
                }
                else
                {
                    tutorialAreas[i].canShowAreaTutorial = !tutorialAreas[i].isActionDependentTutorial;
                }

                if (i % 2 == 1)
                    yield return null;
            }
        }
        else
        {
            yield return null;
            // ✅ STEP 1: Detect old tutorial (PlayerPrefs)
            //bool hasOldTutorialKey = PlayerPrefs.HasKey("completeIndex");
            //int oldIndex = PlayerPrefs.GetInt("completeIndex", -1); // safer default = -1

            //// ✅ STEP 2: Skip new tutorial only if user played any old tutorial step
            //if (hasOldTutorialKey && oldIndex >= 1)
            //{

            //    isTutorialComplete = true;

            //    // Optional: Save tutorial progress in new system for future-proofing
            //    // SaveLegacyTutorialAsComplete();

            //    // Clean up old PlayerPrefs (only once)
            //    PlayerPrefs.DeleteKey("completeIndex");
            //    PlayerPrefs.DeleteKey("Tutorial");
            //    Debug.Log("Old user detected with old tutorial progress. Skipping new tutorial.   " + isTutorialComplete);
            //    yield break;
            //}
          //  Debug.Log("XXXXXXXXXXXXXXXXXXXXXXXXXXX   " + PlayerDataManager.instance.playerData.playerXP);

            for (int i = 0; i < tutorialAreas.Length; i++)
            {
                tutorialAreas[i].canShowAreaTutorial = !tutorialAreas[i].isActionDependentTutorial;
            }
        }

        yield return null;
        isDataInitialized = true;
        GameController.instance.gameData.tutorialBuild = 2;
        //if (IsAreaTutorialComplete("Order_Tut_Item") && PlayerPrefs.GetInt(DataResetKey, 0) == 0)
        //{
        //    print("sssssccccc");
        //    isTutorialComplete = true;
        //    MarkTrueEveryMethod();
        //}
        //if(!isTutorialComplete)
        //{
        //    print("sssssccccc22");
        //    ResetDataIfTaskIncomplete("Order_Tut_Item", "B1Place_Complete", _savedTutorial != null);
        //}
        if(_savedTutorial==null)
        {
            PlayerPrefs.SetInt(DataResetKey, 1);
        }
    }
    public void MarkTrueEveryMethod()
    {
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if(tutorialAreas[i].areaTutorialComplete==false)
            {
                tutorialAreas[i].areaTutorialComplete = true;
                SaveTutorialData();
            }
        }
    }

    public void CheckDependentTutorialsToShow()
    {
        //if (!IsAreaTutorialComplete("SuperStore"))
        //{
        //    return;
        //}
        for (int i = 9; i < tutorialAreas.Length; i++)
        {
            //if (tutorialAreas[i].areadId == "Cafe")
            //{
            //    if (!tutorialAreas[i].areaTutorialComplete)
            //    {
            //        tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.laundaryOpenStatus;
            //        //tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.carwasherOpenStatus;
            //    }
            //}
            //else if (tutorialAreas[i].areadId == "CarWasher")
            //{
            //    if (!tutorialAreas[i].areaTutorialComplete)
            //    {
            //        tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.poolOpenStatus;
            //    }
            //}
            //else if (tutorialAreas[i].areadId == "Pool")
            //{
            //    if (!tutorialAreas[i].areaTutorialComplete)
            //    {
            //        tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.gymOpenStatus;
            //    }
            //}
            //else if (tutorialAreas[i].areadId == "Gym")
            //{
            //    if (!tutorialAreas[i].areaTutorialComplete)
            //    {
            //        tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.laundaryOpenStatus;
            //    }
            //}
            //else
            //if (tutorialAreas[i].areadId == "Laundry")
            //{
            //    if (!tutorialAreas[i].areaTutorialComplete)
            //    {
            //        tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.stationOpenStatus;
            //    }
            //}
            //else if (tutorialAreas[i].areadId == "GasStation")
            //{
            //    if (!tutorialAreas[i].areaTutorialComplete)
            //    {
            //        tutorialAreas[i].canShowAreaTutorial = GameController.instance.gameData.superStoreOpenStatus;
            //    }
            //}
        }
    }
    IEnumerator CheckPreviousProgress(int _progressStartIndex)
    {

        if (_progressStartIndex >= tutorialAreas.Length)
        {
            yield break;
        }
        for (int i = tutorialAreas.Length - 1; i > _progressStartIndex; i--)
        {

            if(tutorialAreas[i].areadId== "Cafe")
            {
                //if (GameController.instance.gameData.bankOpenStatus)
                //{
                //    if (RoomManager.instance.IsAnyRoomReady())
                //    {
                //        //print("Room ready find and tutorial start from 6 but motelOpenStatus true "); 
                //        SetTutorialAreasToComplete(6);

                //        if (RoomManager.instance.IsAnyCustomerServed())
                //        {
                //            tutorialAreas[6].canShowAreaTutorial = true;
                //            tutorialAreas[6].areaTutorialComplete = true;
                //        }
                //        if (RoomManager.instance.IsFirstRoomDirty())
                //        {
                //            tutorialAreas[7].canShowAreaTutorial = true;
                //        }
                //    }
                //    else
                //    {
                //        //print("Room data is reset from motelOpenStatus true");
                //        yield return null;
                //        yield return StartCoroutine(RoomManager.instance.ResetRoom(0));
                //        yield return null;
                //        yield return StartCoroutine(ItemsSavingManager.instance.RemoveRoomItems(0));
                //        GameController.instance.gameData.motelOpenStatus = false;
                //        PlayerDataManager.instance.UpdateCash(1000);
                //    }

                //}
                //else if (RoomManager.instance.IsAnyRoomReady())
                //{
                //    //print("Room ready find and tutorial start from 5 but motelOpenStatus false");
                //    SetTutorialAreasToComplete(5);
                //    if (RoomManager.instance.IsAnyCustomerServed())
                //    {
                //        tutorialAreas[6].canShowAreaTutorial = true;
                //        tutorialAreas[6].areaTutorialComplete = true;
                //    }
                //    if (RoomManager.instance.IsFirstRoomDirty())
                //    {
                //        tutorialAreas[7].canShowAreaTutorial = true;
                //    }
                //}
                //else
                //{
                //    //print("Room data is reset from motelOpenStatus false");
                //    yield return null;
                //    yield return StartCoroutine(RoomManager.instance.ResetRoom(0));
                //    yield return null;
                //    yield return StartCoroutine(ItemsSavingManager.instance.RemoveRoomItems(0));
                //    PlayerDataManager.instance.UpdateCash(1000);
                //}
                //yield break;

            }

        }
    }
    public void SetTutorialAreasToComplete(int _count)
    {
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (i < _count)
            {
                tutorialAreas[i].areaTutorialComplete = true;
            }
        }

    }
    public void SaveTutorialData(/*bool _saveOnLeaving*/)
    {
        if (!isDataInitialized)
        {
            return;
        }
        //if (_saveOnLeaving)
        //{
        //if (IsAreaTutorialComplete("Room_1", "R1_PaintOrder"))
        //{
        //    if (DeliveryTruckManager.instance != null)
        //    {
        //        if (DeliveryTruckManager.instance.truckData.deliveryTruckPlacedItems.Count <= 0)
        //        {
        //            SetSubTaskToComplete("Room_1", "R1_OrderBlitz", false);
        //        }
        //    }
        //    else
        //    {
        //        SetSubTaskToComplete("Room_1", "R1_OrderBlitz", false);

        //    }
        //}
        //if (IsAreaTutorialComplete("Room_1_Item", "R1_ItemOrder"))
        //{
        //    if (DeliveryTruckManager.instance != null)
        //    {
        //        if (DeliveryTruckManager.instance.truckData.deliveryTruckPlacedItems.Count <= 0)
        //        {
        //            SetSubTaskToComplete("Room_1_Item", "R1_ItemOrderWait", false);
        //        }
        //    }
        //    else
        //    {
        //        SetSubTaskToComplete("Room_1_Item", "R1_ItemOrderWait", false);
        //    }

        //}



        TutorialSavable _dataToSave = new TutorialSavable();
        _dataToSave.tutorialBuildIndex = 2;
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            TutorialSavableArea _area = new TutorialSavableArea(tutorialAreas[i].areadId);
            _area.isTutorialCompleted = tutorialAreas[i].areaTutorialComplete;
            _area.canShowAreaTutorial = tutorialAreas[i].canShowAreaTutorial ? true : !tutorialAreas[i].isActionDependentTutorial;
            for (int j = 0; j < tutorialAreas[i].tutorialTasks.Count; j++)
            {
                if (tutorialAreas[i].tutorialTasks[j].taskComplete)
                {
                    int _tmp = j;
                    _area.taskIds.Add(tutorialAreas[i].tutorialTasks[_tmp].taskId);
                }
            }
            _dataToSave.areas.Add(_area);
        }
        SerializationManager.Save(_dataToSave, "TutorialData");
    }
    #endregion

    #region Callings
    public int IsAnyActiveTutorial()
    {
        if (currentArea == null)
        {
            print("Area is null ");
            return 0;
        }
        if (string.IsNullOrEmpty(currentArea.areadId))
        {
            print("Area ID is null ");
            return 0;
        }
        if (!currentArea.highPriorityTutorial)
        {
            return 1;
        }
        return 2;
    }
    public void CheckToDisplayTutorial() // yeh game Start hony py check krta ha tutorial konsa enable hona ha
    {
        // print("iiiiiiiiiiiiFFFFFF  " + isTutorialComplete);
       
        if (isTutorialComplete)
            return;
       // print("elseeeeeeee  " + isTutorialComplete);
        currentArea = null;
        bool _anyActionDependantArea = false;
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (!tutorialAreas[i].areaTutorialComplete)
            {
                if (!tutorialAreas[i].canShowAreaTutorial)
                {
                    _anyActionDependantArea = true;
                }
                else
                {
                    currentArea = tutorialAreas[i];
                    break;
                }
            }
        }
        if (currentArea != null)
        {
           // print("ccccccccccccc  " + isTutorialComplete);
            //Display Heading For Current Area In Quest Panel Here
            DisplayCurrentAreaTutorialTask();

            //if (currentArea.areadId == "Serve_AtmCustomer")
            //{
            //    WarehouseManager.instance.AddBalanceAtmForTutorial();
            //}
        }
        else
        {
            // print("bbbbbbbbbbbbb  " + isTutorialComplete);
            ObjectiveRequested?.Invoke();
            isTutorialComplete = !_anyActionDependantArea;
            TutorialCompleted?.Invoke();
           // print("tutFFFFFF  " + isTutorialComplete);
        }
    }
    public void SetAreaTutorialToShow(string _areaId)
    {
        if (isTutorialComplete)
        {
            return;
        }
        if (_areaId != "Room_Cleaning")
        {
            if (!IsAreaTutorialComplete("Room_Cleaning"))
            {
                return;
            }
            if (_areaId != "SuperStore")
            {
                if (!IsAreaTutorialComplete("SuperStore"))
                {
                    return;
                }
            }
        }
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                tutorialAreas[i].canShowAreaTutorial = true;
                if (tutorialAreas[i].areaTutorialComplete)
                {
                    return;
                }
                if (currentArea == null)
                {
                    CheckToDisplayTutorial();
                    return;
                }
                else
                {
                    if (string.IsNullOrEmpty(currentArea.areadId))
                    {
                        CheckToDisplayTutorial();
                        return;
                    }
                }
            }
        }
    }
    public void DisplayCurrentAreaTutorialTask()
    {
        for (int i = 0; i < currentArea.tutorialTasks.Count; i++)
        {
            if (!currentArea.tutorialTasks[i].taskComplete)
            {
                for (int j = 0; j < currentArea.tutorialTasks[i].subTasks.Count; j++)
                {
                    if (!currentArea.highPriorityTutorial)//by ss
                    {
                        ObjectiveRequested?.Invoke();
                        return;
                    }
                    if (!currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                    {
                        DisplaySubTask(currentArea.tutorialTasks[i].subTasks[j]);
                        return;
                    }
                }
            }
        }
        currentArea.areaTutorialComplete = true;
        CheckToDisplayTutorial();
    }

    public void DisplaySubTask(TutorialSubTask _subTask)
    {
        Debug.Log("isssssssssssssssssss");
        tutorialOverlayPanel.SetActive(_subTask.displayUIOverlayScreen);
        overlayTapPanel.SetActive(_subTask.enableScreenTap);
        overlayInstructionCntnr.gameObject.SetActive(_subTask.displayOverlayInstruction);
        overlayInstructionTxt.GetComponent<LocalizeText>().UpdateText(_subTask.taskInstruction);
        overlayInstructionTapPress.GetComponent<LocalizeText>().UpdateText(_subTask.overlayTapInstruction);
        if (_subTask.subTaskUIObject != null)
        {
            _subTask.subTaskUIObject.SetActive(true);
        }
        if (_subTask.subTaskObject != null)
        {
            _subTask.subTaskObject.SetActive(true);
        }
        if (_subTask.updateQuestInstruction)
        {
            //Update Quest Instruction From Here
            bool _instantDisplay = true;
            if (currentArea != null && !currentArea.highPriorityTutorial)
            {
                _instantDisplay = !(IsObjectiveActive != null && IsObjectiveActive());
            }

            QuestInstructionRequested?.Invoke(_subTask.questInstruction, _subTask.questHeading, true, _subTask.displayQuestOnTop, _instantDisplay);

            //  UIController.instance.DisplayQuestInstruction(_subTask.questInstruction, _subTask.questHeading, true, _subTask.displayQuestOnTop);
        }
        if (!string.IsNullOrEmpty(_subTask.dynamicBtnId))
        {

            string[] _split = _subTask.dynamicBtnId.Split("_");
            if (_split.Length > 1)
            {
                List<string> _ids = new List<string>();
                for (int i = 1; i < _split.Length; i++)
                {
                    string _id = _split[i];
                    _ids.Add(_id);
                }
                if (_split[0] == "Store")
                {
                    _subTask.btnToBeCutOut = DynamicButtonResolver?.Invoke(_ids);
                }
                //else if (_split[0] == "MotelReception")
                //{
                //    _subTask.btnToBeCutOut = ReceptionUIManager.Instance.GetRoomButton(int.Parse(_split[1]));
                //}
                //else if (_split[0] == "StoreCustomerBilling")
                //{
                //    RectTransform _rect = SuperStoreManager.instance.GetExpectedBtnToInteract();
                //    if (_rect != null)
                //    {
                //        Debug.LogError("_react is not null");
                //        _subTask.btnToBeCutOut = _rect.gameObject;
                //    }
                //    else
                //    {
                //        Debug.LogError("_react is null");
                //    }
                //}
            }
            if (_subTask.displayUIOverlayScreen && _subTask.btnToBeCutOut == null)
            {
                Debug.LogError("Cut Out Image Not Found");
            }
        }
        if (!string.IsNullOrEmpty(_subTask.dynamicItemId))
        {
           // print("_subTask.dynamicItemId :" + _subTask.dynamicItemId);
            string[] _split = _subTask.dynamicItemId.Split("_");
            if (_split.Length > 0)
            {
                List<string> _ids = new List<string>();

                for (int i = 1; i < _split.Length; i++)
                {
                    string _id = _split[i];
                    _ids.Add(_id);
                }
                if (_split[0] == "Box")
                {
                   // print("IDs is : " + _ids);
                    _subTask.taskTarget = TargetResolver?.Invoke("Box", _ids);

                    if (_subTask.isDrawPath)
                    {
                        if(_subTask.objectiveTarget==null)
                        {
                            _subTask.objectiveTarget = _subTask.taskTarget;//by ss
                            print("sssssssssssssssssyyyyyyyy");
                        }
                        if (_subTask.objectiveTarget!= null)
                        {
                            if (navmeshPathDraw != null) navmeshPathDraw.setDestinationAndDrawPath(_subTask.objectiveTarget.transform);

                        }
                    }
                    if (_subTask.isSpawnParticleEffect && _subTask.particleEffect != null)
                    {
                        spawnEffect = Instantiate(
                            _subTask.particleEffect,
                            _subTask.pos,
                            _subTask.rot
                        );

                        spawnEffect.SetActive(true);
                    }
                    if (!_subTask.isSpawnParticleEffect && spawnEffect != null)
                    {
                        spawnEffect.SetActive(false);
                        Destroy(spawnEffect);
                    }
                }
                else if (_split[0] == "Item")
                {
                    _subTask.taskTarget = TargetResolver?.Invoke("Item", _ids);
                    _subTask.objectiveTarget= _subTask.taskTarget;
                }
                if (_subTask.taskTarget == null)
                {
                    string _idTest = "";
                    for (int i = 0; i < _ids.Count; i++)
                    {
                        _idTest += _ids[i] + "-";
                    }
                    //Debug.LogError(_idTest);
                }
            }

        }
        if (!string.IsNullOrEmpty(_subTask.dynamicParcelId))
        {
            // Directly handle only Parcel_true / Parcel_false
            if (_subTask.dynamicParcelId.Equals("Parcel_true", StringComparison.OrdinalIgnoreCase))
            {
                // ✅ When Parcel is true
                Debug.Log("Dynamic Parcel condition: TRUE");
                // Example: Assign a target or do your logic here
               // _subTask.taskTarget = ParcelManager.instance.GetParcel(true);
            }
            else if (_subTask.dynamicParcelId.Equals("Parcel_false", StringComparison.OrdinalIgnoreCase))
            {
                // ❌ When Parcel is false
                Debug.Log("Dynamic Parcel condition: FALSE");
                // Example: Assign a target or do your logic here
                //_subTask.taskTarget = ParcelManager.instance.GetParcel(false);
            }
            else
            {
                Debug.LogWarning($"Unexpected dynamicParcelId value: {_subTask.dynamicParcelId}");
            }
        }

        if (_subTask.displayNavigationToTarget)
        {
            if (_subTask.taskTarget != null && _subTask.taskTarget.gameObject.activeSelf == true)
            {

                if (_subTask.delayNavigate)
                {
                    if (navgationDelay != null)
                    {
                        StopCoroutine(navgationDelay);
                    }
                    navgationDelay = StartCoroutine("NavgationDelay", (_subTask.taskTarget));
                }
                else
                {
                    if (_subTask.isDrawPath)

                    {

                        if (navmeshPathDraw != null) navmeshPathDraw.setDestinationAndDrawPath(_subTask.objectiveTarget.transform);

                    }
                    targetPointer.transform.position = _subTask.taskTarget.position;
                    //targetPointer.transform.rotation = _subTask.taskTarget.rotation;

                    targetPointer.SetActive(true);
                    targetArrow.GetComponent<Outline>().enabled = true;
                    targetArrow.GetComponent<MeshRenderer>().enabled = false;////by ss
                    if (_subTask.isSpawnParticleEffect && _subTask.particleEffect != null)
                    {
                        spawnEffect = Instantiate(
                            _subTask.particleEffect,
                            _subTask.pos,
                            _subTask.rot
                        );

                        spawnEffect.SetActive(true);
                    }
                    if (!_subTask.isSpawnParticleEffect && spawnEffect != null)
                    {
                        spawnEffect.SetActive(false);
                        Destroy(spawnEffect);
                    }
                }
            }

        }
        if (_subTask.displayUIOverlayScreen)
        {
            Color _color = Color.black;
            _color.a = _subTask.fadeOverlayScreen ? 0.6f : 0f;
            tutorialOverlayPanel.GetComponent<Image>().color = _color;
            if (_subTask.delayCutOut)
            {
                if (delayedCutout != null)
                {
                    StopCoroutine(delayedCutout);
                }
                delayedCutout = StartCoroutine("SetDelayedCutoutTarget", (_subTask.btnToBeCutOut.GetComponent<RectTransform>()));

            }
            else
            {
                SetCutOutImageToTarget(_subTask.btnToBeCutOut.GetComponent<RectTransform>());
            }

        }
        if (_subTask.displayHandAnim)
        {
            string _anim = (_subTask.isTapPressedAnim ? "Tap" : "Point");
            animHandObj.gameObject.SetActive(true);
            SetHandAnimToTarget(_subTask.btnToBeCutOut.GetComponent<RectTransform>(), _anim, _subTask.unchildhandAnim);

        }

    }
    Coroutine navgationDelay;
    IEnumerator NavgationDelay(Transform _target)
    {
        yield return new WaitForSeconds(1f);// by ss orignal time 1f
        targetPointer.transform.position = _target.position;
        //targetPointer.transform.rotation = _subTask.taskTarget.rotation;
        targetPointer.SetActive(true);
        targetArrow.GetComponent<Outline>().enabled = true;
        targetArrow.GetComponent<MeshRenderer>().enabled = true;//////////by ss
        navgationDelay = null;
    }
    Coroutine delayedCutout;
    IEnumerator SetDelayedCutoutTarget(RectTransform _target)
    {
        yield return new WaitForSeconds(1f);
        SetCutOutImageToTarget(_target);
        delayedCutout = null;
    }
    void SetCutOutImageToTarget(RectTransform _target)
    {
        var _cutOutImage = cutOutObject.GetComponent<Image>();
        var _targetImage = _target.GetComponent<Image>();
        _cutOutImage.sprite = _targetImage.sprite;
        _cutOutImage.pixelsPerUnitMultiplier = _targetImage.pixelsPerUnitMultiplier;   // a sliced target (panel) keeps its border size
        cutOutObject.transform.SetParent(_target.transform.parent);

        cutOutObject.anchorMin = _target.anchorMin;
        cutOutObject.anchorMax = _target.anchorMax;
        cutOutObject.pivot = _target.pivot;
        cutOutObject.sizeDelta = _target.sizeDelta;
        cutOutObject.localEulerAngles = _target.localEulerAngles;
        cutOutObject.anchoredPosition = _target.anchoredPosition;

        Vector2 _instructionPosition = _target.anchoredPosition;
        Vector2 _size = cutOutObject.sizeDelta;
        float _xAnchor = _target.anchorMin.x;
        float _yAnchor = _target.anchorMin.y;

        bool isLeft = _xAnchor < 0.25f;
        bool isCenter = _xAnchor < 0.75f && !isLeft;
        bool isRight = !isLeft && !isCenter;

        bool isBottom = _yAnchor < 0.25f;
        bool isMiddle = _yAnchor < 0.75f && !isBottom;
        bool isTop = !isBottom && !isMiddle;

        if (isLeft)
        {
            _instructionPosition.x += 30f;
            _instructionPosition.y += isBottom ? (_size.y + 20f) : isTop ? -(_size.y + 20f) : 0;
            if (isMiddle) _instructionPosition.x += (_size.x + 40f);
        }
        else if (isCenter)
        {
            _instructionPosition.y += isBottom ? (_size.y + 20f) : -(_size.y + 20f);
        }
        else if (isRight)
        {
            _instructionPosition.x += -30f;
            _instructionPosition.y += isBottom ? (_size.y + 20f) : isTop ? -(_size.y + 30f) : 0;
            if (isMiddle) _instructionPosition.x -= (_size.x + 40f);
        }
        cutOutObject.gameObject.SetActive(true);

        if (!cutoutResetRoutineActive)
        {
            cutoutResetRoutineActive = true;
            StartCoroutine(ResetCutOutObject());
        }

    }
    bool cutoutResetRoutineActive = false;
    IEnumerator ResetCutOutObject()
    {
        yield return null;
        yield return null;
        cutOutObject.SetParent(cutOutObjectParent);
        cutOutObject.SetAsFirstSibling();
        cutoutResetRoutineActive = false;
    }
    void SetHandAnimToTarget(RectTransform _target, string _animToPlay, bool _unchildAnim)
    {
        animHandObj.transform.SetParent(_target.transform);

        float _xAnchor = _target.pivot.x;
        float _yAnchor = _target.pivot.y;

        bool isLeft = _xAnchor < 0.25f;
        bool isCenter = _xAnchor < 0.75f && !isLeft;
        bool isRight = !isLeft && !isCenter;

        bool isBottom = _yAnchor < 0.25f;
        bool isMiddle = _yAnchor < 0.75f && !isBottom;
        bool isTop = !isBottom && !isMiddle;
        int _angleVal = 0;

        if (_animToPlay == "Point")
        {
            if (isCenter && isMiddle)
            {
                _angleVal = 0;
            }
            if (isLeft)
            {
                _angleVal = 2;
            }
            else if (isCenter)
            {
                if (isBottom)
                    _angleVal = 3;
                else if (isMiddle || isTop)
                {
                    _angleVal = 1;
                }
            }
            else if (isRight)
            {
                if (isBottom)
                    _angleVal = 3;
                else if (isMiddle || isTop)
                {
                    _angleVal = 1;
                }
            }
            _angleVal = _angleVal * 90;
        }
        else
        {
            if (isLeft)
            {
                if (isBottom)
                    _angleVal = 5;
                else if (isCenter || isTop)
                    _angleVal = 2;
            }
            else if (isCenter)
            {
                if (isBottom)
                    _angleVal = 5;
                else if (isMiddle || isTop)
                {
                    _angleVal = 1;
                }
            }
            else if (isRight)
            {
                if (isBottom)
                    _angleVal = 7;
                else if (isMiddle || isTop)
                {
                    _angleVal = 1;
                }
            }
            _angleVal = _angleVal * 45;
        }

        Vector3 _angle = Vector3.zero;
        _angle.z = _angleVal;

        animHandObj.eulerAngles = _angle;
        animHandObj.anchoredPosition = Vector3.zero;
        animHandObj.GetComponent<Animator>().Play(_animToPlay);
        if (_unchildAnim)
        {
            animHandObj.SetParent(handAnimDefaultParent);
        }
    }
    public void OnActionBtnPressed(string _actionId, UnityAction _action = null)
    {
        if (string.IsNullOrEmpty(_actionId))
        {
            return;
        }
        if (currentArea == null)
        {
            return;
        }
        if (isTutorialComplete)
        {
            return;
        }
        if (currentArea.tutorialTasks == null)
        {
            return;
        }
        for (int i = 0; i < currentArea.tutorialTasks.Count; i++)
        {
            if (!currentArea.tutorialTasks[i].taskComplete)
            {
                for (int j = 0; j < currentArea.tutorialTasks[i].subTasks.Count; j++)
                {
                    if (!currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                    {
                        if (currentArea.tutorialTasks[i].subTasks[j].actionBtnId == _actionId)
                        {
                            OnSubTaskComplete(currentArea.tutorialTasks[i].taskId, currentArea.tutorialTasks[i].subTasks[j].subTaskId, currentArea.areadId, _action);
                        }
                        return;
                    }
                }
            }
        }
    }
    public void OnSubTaskComplete(string _taskId, string _subTaskId, string _areaId = null, UnityAction _action = null)
    {
        print("Taskid : " + _taskId + " _subTaskId : " + _subTaskId + " _areaId : " + _areaId);
        if (_areaId == null && currentArea == null)
        {
            return;
        }
        AreaTutorials _currentArea = currentArea;
        if (_areaId != null)
        {
            _currentArea = null;
            for (int i = 0; i < tutorialAreas.Length; i++)
            {
                if (tutorialAreas[i].areadId == _areaId)
                {
                    if (tutorialAreas[i].areaTutorialComplete)
                    {
                        return;
                    }
                    _currentArea = tutorialAreas[i];
                    break;
                }
            }
        }
        if (_currentArea == null)
        {
            return;
        }
        bool _isCurrentActiveTask = false;
        if (currentArea != null && !string.IsNullOrEmpty(currentArea.areadId))
        {
            _isCurrentActiveTask = (_currentArea.areadId == currentArea.areadId);
            if (_isCurrentActiveTask)
            {
                _isCurrentActiveTask = false;
                for (int i = 0; i < currentArea.tutorialTasks.Count; i++)
                {
                    if (!currentArea.tutorialTasks[i].taskComplete)
                    {
                        if (currentArea.tutorialTasks[i].taskId == _taskId)
                        {
                            for (int k = 0; k < currentArea.tutorialTasks[i].subTasks.Count; k++)
                            {
                                if (!currentArea.tutorialTasks[i].subTasks[k].subTaskComplete)
                                {
                                    _isCurrentActiveTask = currentArea.tutorialTasks[i].subTasks[k].subTaskId == _subTaskId;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

        }
        for (int i = 0; i < _currentArea.tutorialTasks.Count; i++)
        {
            if (_currentArea.tutorialTasks[i].taskId == _taskId)
            {
                if (_currentArea.tutorialTasks[i].taskComplete)
                {
                    break;
                }
                int _notCompletedIndex = -1;
                bool _subTaskUpdated = false;
                for (int j = 0; j < _currentArea.tutorialTasks[i].subTasks.Count; j++)
                {
                    if (_currentArea.tutorialTasks[i].subTasks[j].subTaskId == _subTaskId)
                    {
                        if (_currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                            return;

                        //GameManager.instance.CallFireBase(_currentArea.areadId + i.ToString() + "_" + j.ToString());
                        _currentArea.tutorialTasks[i].subTasks[j].subTaskComplete = true;
                        if (_currentArea.tutorialTasks[i].subTasks[j].isDrawPath)

                        {
                            print("ssstststtststststs");
                            if (navmeshPathDraw != null) navmeshPathDraw.Stop();//by ss

                        }
                        if (_currentArea.tutorialTasks[i].subTasks[j].isSpawnParticleEffect)
                        {
                            if (spawnEffect != null)
                            {
                                print("ssstststtststststs------------");
                                spawnEffect.SetActive(false);
                                Destroy(spawnEffect.gameObject);
                            }
                        }
                        if (_isCurrentActiveTask)
                        {
                            cutOutObject.gameObject.SetActive(false);
                            animHandObj.gameObject.SetActive(false);
                            tutorialOverlayPanel.SetActive(false);
                            overlayInstructionCntnr.gameObject.SetActive(false);
                            overlayTapPanel.SetActive(false);
                            if (_currentArea.tutorialTasks[i].subTasks[j].subTaskUIObject != null)
                            {
                                _currentArea.tutorialTasks[i].subTasks[j].subTaskUIObject.SetActive(false);
                            }
                            if (_currentArea.tutorialTasks[i].subTasks[j].subTaskObject != null)
                            {
                                _currentArea.tutorialTasks[i].subTasks[j].subTaskObject.SetActive(false);
                            }
                            if (_currentArea.tutorialTasks[i].subTasks[j].updateQuestInstruction)
                            {
                                QuestInstructionRemoved?.Invoke();
                            }
                            if (currentArea.tutorialTasks[i].subTasks[j].delayCutOut)
                            {
                                if (delayedCutout != null)
                                {
                                    StopCoroutine(delayedCutout);
                                }
                            }
                            targetPointer.SetActive(false);
                        }
                        _subTaskUpdated = true;
                    }
                    if (_notCompletedIndex < 0 && !_currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                    {
                        _notCompletedIndex = j;
                    }

                    if (_subTaskUpdated && _notCompletedIndex >= 0)
                    {
                        break;
                    }
                }
                if (_subTaskUpdated)
                {
                    if (_notCompletedIndex >= 0)
                    {
                        if (_isCurrentActiveTask)
                        {
                            DisplaySubTask(_currentArea.tutorialTasks[i].subTasks[_notCompletedIndex]);
                        }
                    }
                    else
                    {
                        _currentArea.tutorialTasks[i].taskComplete = true;
                        if (_isCurrentActiveTask)
                        {
                            GameManager.instance.CallFireBase(_currentArea.areadId + "_" + _currentArea.tutorialTasks[i].taskId + "_Cmplt");
                            OnCompleteAreaTask();
                        }
                    }
                    _action?.Invoke();
                    return;
                }
            }
        }
    }
    void OnCompleteAreaTask()
    {
        for (int i = 0; i < currentArea.tutorialTasks.Count; i++)
        {
            if (!currentArea.tutorialTasks[i].taskComplete)
            {
                for (int j = 0; j < currentArea.tutorialTasks[i].subTasks.Count; j++)
                {
                    if (!currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                    {
                        DisplaySubTask(currentArea.tutorialTasks[i].subTasks[j]);
                        return;
                    }
                }
                //This will only call when sub tasks of a task are all completed but Task is not marked as complete
                currentArea.tutorialTasks[i].taskComplete = true;
            }
        }
        //This will only call when not uncompleted task found in this area
        currentArea.areaTutorialComplete = true;
        CheckToDisplayTutorial();
    }
    public void OnCompleteSpecifiTask(string _taskID)
    {
        for (int i = 0; i < currentArea.tutorialTasks.Count; i++)
        {
            if (currentArea.tutorialTasks[i].taskId == _taskID)
            {
                if (!currentArea.tutorialTasks[i].taskComplete)
                {
                    for (int j = 0; j < currentArea.tutorialTasks[i].subTasks.Count; j++)
                    {
                        if (!currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                        {
                            currentArea.tutorialTasks[i].subTasks[j].subTaskComplete = true;
                        }
                    }
                }
                currentArea.tutorialTasks[i].taskComplete = true;
            }
        }
        CheckToDisplayTutorial();
    }
    public void SetSubTaskToComplete(string _areaId, string _taskId = null, bool _checkForNextTask = true)
    {
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                if (tutorialAreas[i].areaTutorialComplete)
                {
                    return;
                }
                if (_taskId == null)
                {
                    tutorialAreas[i].areaTutorialComplete = true;
                }
                bool _firstInCompleteTask = true;
                for (int j = 0; j < tutorialAreas[i].tutorialTasks.Count; j++)
                {
                    if (_taskId == null)
                    {
                        tutorialAreas[i].tutorialTasks[j].taskComplete = true;
                        continue;
                    }
                    if (tutorialAreas[i].tutorialTasks[j].taskId == _taskId)
                    {
                        if (tutorialAreas[i].tutorialTasks[j].taskComplete)
                        {
                            return;
                        }

                        for (int k = 0; k < tutorialAreas[i].tutorialTasks[j].subTasks.Count; k++)
                        {
                            if (currentArea != null)
                            {
                                if (currentArea.areadId == _areaId && _firstInCompleteTask && tutorialAreas[i].canShowAreaTutorial
                                && !tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskComplete)
                                {
                                    cutOutObject.gameObject.SetActive(false);
                                    animHandObj.gameObject.SetActive(false);
                                    tutorialOverlayPanel.SetActive(false);
                                    overlayInstructionCntnr.gameObject.SetActive(false);
                                    overlayTapPanel.SetActive(false);

                                    if (tutorialAreas[i].tutorialTasks[j].subTasks[k].updateQuestInstruction)
                                    {
                                        QuestInstructionRemoved?.Invoke();
                                    }

                                    targetPointer.SetActive(false);
                                }
                            }
                            tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskComplete = true;
                            if (tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskUIObject != null)
                            {
                                tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskUIObject.SetActive(false);
                            }
                            if (tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskObject != null)
                            {
                                tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskObject.SetActive(false);
                            }
                        }
                        tutorialAreas[i].tutorialTasks[j].taskComplete = true;
                        GameManager.instance.CallFireBase(tutorialAreas[i].areadId + "_" + tutorialAreas[i].tutorialTasks[j].taskId + "_Cmplt");

                        if (currentArea != null && _checkForNextTask)
                        {
                            if (currentArea.areadId == _areaId)
                            {
                                OnCompleteAreaTask();
                            }
                        }
                        return;
                    }
                    if (_firstInCompleteTask && !tutorialAreas[i].tutorialTasks[j].taskComplete)
                    {
                        _firstInCompleteTask = false;
                    }
                }
            }
        }
    }

    public void MarkPreviousTasksComplete(string _areaId, string _taskId = null)
    {
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                if (tutorialAreas[i].areaTutorialComplete)
                {
                    return;
                }
                for (int j = 0; j < tutorialAreas[i].tutorialTasks.Count; j++)
                {
                    tutorialAreas[i].tutorialTasks[j].taskComplete = true;
                    if (tutorialAreas[i].tutorialTasks[j].taskId == _taskId)
                    {
                        if (currentArea != null)
                        {
                            if (currentArea.areadId == _areaId)
                            {
                                OnCompleteAreaTask();
                            }
                        }
                        return;
                    }
                }
            }
        }
    }
    public void SetAreaTaskToIncomplete(string _areaId, string _taskId)
    {
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                if (tutorialAreas[i].areaTutorialComplete)
                {
                    return;
                }
                for (int j = 0; j < tutorialAreas[i].tutorialTasks.Count; j++)
                {
                    if (tutorialAreas[i].tutorialTasks[j].taskId == _taskId)
                    {
                        if (tutorialAreas[i].tutorialTasks[j].taskComplete)
                        {
                            tutorialAreas[i].tutorialTasks[j].taskComplete = false;
                        }
                        return;
                    }
                }
            }
        }
    }
    public bool IsAreaTutorialComplete(string _areaId, string _taskId, string _subTaskId = null) // check the compeleted task of given ids
    {
        //return true;
        if (isTutorialComplete)
            return true;
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                if (tutorialAreas[i].areaTutorialComplete)
                    return true;
                for (int j = 0; j < tutorialAreas[i].tutorialTasks.Count; j++)
                {
                    if (tutorialAreas[i].tutorialTasks[j].taskId == _taskId)
                    {
                        if (tutorialAreas[i].tutorialTasks[j].taskComplete)
                        {
                            return true;
                        }
                        if (_subTaskId == null)
                        {
                            return tutorialAreas[i].tutorialTasks[j].taskComplete;
                        }
                        else
                        {
                            for (int k = 0; k < tutorialAreas[i].tutorialTasks[j].subTasks.Count; k++)
                            {
                                if (tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskId == _subTaskId)
                                    return tutorialAreas[i].tutorialTasks[j].subTasks[k].subTaskComplete;
                            }
                            return tutorialAreas[i].tutorialTasks[j].taskComplete;
                        }
                    }
                }
            }
        }
        return false;
    }
    public bool IsAreaTutorialComplete(string _areaId) // this method check the area tutorial completed of given id
    {
        //return true;
        if (isTutorialComplete)
            return true;
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                return tutorialAreas[i].areaTutorialComplete;
            }
        }
        return false;
    }
    public bool IsAreaTutorialHighPriority(string _areaId) // this method check the area tutorial is highpriorty or not
    {
        if (isTutorialComplete)
            return false;
        for (int i = 0; i < tutorialAreas.Length; i++)
        {
            if (tutorialAreas[i].areadId == _areaId)
            {
                return tutorialAreas[i].highPriorityTutorial;
            }
        }
        return false;
    }
    #endregion
    public void OnTutorialInstructionRead(string _subTaskId)
    {
        OnSubTaskComplete("Info_Task", _subTaskId);
    }
    public void OnTutorialInstructionTapped()
    {
        if (currentArea != null)
        {
            for (int i = 0; i < currentArea.tutorialTasks.Count; i++)
            {
                if (!currentArea.tutorialTasks[i].taskComplete)
                {
                    for (int j = 0; j < currentArea.tutorialTasks[i].subTasks.Count; j++)
                    {
                        if (!currentArea.tutorialTasks[i].subTasks[j].subTaskComplete)
                        {
                            if (currentArea.tutorialTasks[i].subTasks[j].enableScreenTap)
                            {
                               // print("onTapped");
                                OnSubTaskComplete(currentArea.tutorialTasks[i].taskId, currentArea.tutorialTasks[i].subTasks[j].subTaskId);
                            }
                            return;
                        }
                    }
                }
            }
        }
    }

    public void UpdateTargetPointer(Transform _target, bool _forceEnable = false)
    {
        targetPointer.transform.position = _target.position;
        if (_forceEnable)
        {
            targetPointer.gameObject.SetActive(true);
        }
    }
    public void UpdateHandPointTarget(RectTransform _target, string _animToPlay, bool _enableCutOut, bool _forceEnable = false)
    {
        if (_forceEnable)
        {
            animHandObj.gameObject.SetActive(true);
        }
        SetHandAnimToTarget(_target, _animToPlay, false);
        if (_enableCutOut)
        {
           // print("Updating Cutout Image");
            tutorialOverlayPanel.SetActive(true);
            SetCutOutImageToTarget(_target);

        }
    }
    ////by SS
    
    public void ArrowMesh(bool _status)
    {
       // targetPointer.SetActive(_status);
       // targetArrow.GetComponent<Outline>().enabled = _status;
        targetArrow.GetComponent<MeshRenderer>().enabled = _status;
    }
}

[System.Serializable]
public class TutorialSavable
{
    public int tutorialBuildIndex;
    public List<TutorialSavableArea> areas;
    public TutorialSavable()
    {
        areas = new List<TutorialSavableArea>();
        tutorialBuildIndex = 2;
    }
}
[System.Serializable]
public class TutorialSavableArea
{
    public string areaId;
    public bool isTutorialCompleted;
    public bool canShowAreaTutorial;
    public List<string> taskIds;

    public TutorialSavableArea(string _areaId)
    {
        areaId = _areaId;
        isTutorialCompleted = false;
        taskIds = new List<string>();
    }
}
[System.Serializable]
public class AreaTutorials
{
    public string areadId;
    public string tutorialHeading;
    public bool areaTutorialComplete;
    public bool canShowAreaTutorial;
    public bool isActionDependentTutorial;
    public bool highPriorityTutorial = true;
    public List<TutorialTask> tutorialTasks;
}
[System.Serializable]
public class TutorialTask
{
    public string taskId;
    public string taskHeading;
    public bool taskComplete;
    public List<TutorialSubTask> subTasks;
}
[System.Serializable]
public class TutorialSubTask
{
    //In Case Of UI Interactions Sub Task Id should remain same for similar steps accross different Task
    public string subTaskId;
    public string actionBtnId;
    public string taskInstruction;
    public string questHeading = "Tutorial";
    public string questInstruction;
    public string overlayTapInstruction = "Tap To Continue";
    public bool subTaskComplete;
    public bool displayNavigationToTarget;
    public bool displayUIOverlayScreen;
    public bool enableScreenTap;
    public bool fadeOverlayScreen;
    public bool displayOverlayInstruction;
    public bool updateQuestInstruction;
    public bool displayQuestOnTop = true;
    public bool displayHandAnim;
    public bool isTapPressedAnim;
    public bool unchildhandAnim;
    public bool delayCutOut;
    public bool delayNavigate;
    public string dynamicBtnId;
    public string dynamicItemId;
    public string dynamicParcelId;
    public Transform taskTarget;
    public GameObject btnToBeCutOut;
    public GameObject subTaskUIObject;
    public GameObject subTaskObject;
    public bool isDrawPath;
    public Transform objectiveTarget;
    public bool isSpawnParticleEffect;
    public GameObject particleEffect;
    public Vector3 pos;
    public Quaternion rot;
}