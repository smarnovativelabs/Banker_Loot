using System.Collections;
using System.Collections.Generic;
using TouchControlsKit;
using UnityEngine;
using UnityEngine.UI;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

public class Controlsmanager : MonoBehaviour
{
    public KinematicCharacterMotor kinematicCharacterMotor;
    public static Controlsmanager instance;
    public TCKTouchpad touchpad;
    public Charactercontroller charactercontroller;
    public Controllerinputmanager controllerinputmanager;
    public FloatingJoystick joystick;
    float characterStoppingSpeed;
    public float characterMoveSpeed;
    public Image crosshair;

    void Awake()
    {
        instance = this;
        characterMoveSpeed = charactercontroller.MaxStableMoveSpeed;
    }

    private void Start()
    {
        StartCoroutine(ActivateControls());
    }

    IEnumerator ActivateControls()
    {
        yield return new WaitForSeconds(.2f);
        ActivateControls(true, characterMoveSpeed);
    }

    public void ChangePlayerPosition(Transform newPosition)
    {
        kinematicCharacterMotor.SetPositionAndRotation(newPosition.position, newPosition.rotation);
    }

    public void UpdateCharacterSpeed(float _speed)
    {
        charactercontroller.setMaxSpeed(_speed);
    }

    //this will enable/diable touchpad interaction
    public void ActivateTouchPad(bool state)
    {
        touchpad.isActive = state;
    }

    public void ActivateControls(bool state, float speed)
    {
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(state);
        }
        ActivateTouchPad(state);

        joystick.gameObject.SetActive(state);
        joystick.SimulatePointerUp();

        charactercontroller.setMaxSpeed(speed);
    }

    /// <summary>
    /// Stop player by passing 0 to this method
    /// </summary>
    public void StopPlayer()
    {
        charactercontroller.setMaxSpeed(characterStoppingSpeed);
    }

    /// <summary>
    /// Move player at the default speed
    /// </summary>
    public void MovePlayer()
    {
        charactercontroller.setMaxSpeed(characterMoveSpeed);
    }

    public void UpdateSensitivity(float value)
    {
        touchpad.sensitivity = value;
    }

    public void AddToIgnoreCollision(BoxCollider _box)
    {
        charactercontroller.IgnoredColliders.Add(_box);
    }

    public void RemoveItemToIgnoreCollision()
    {
        charactercontroller.IgnoredColliders.RemoveAt(charactercontroller.IgnoredColliders.Count - 1);
    }

    public void SpawnPlayerAt(Transform pos)
    {
        ChangePlayerPosition(pos);
        controllerinputmanager.ChangeCameraRotation();
    }
}
