using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using TouchControlsKit;

namespace KinematicCharacterController.Examples
{
    public class Controllerinputmanager : MonoBehaviour
    {
        public Charactercontroller Character;
        public Camerafollow CharacterCamera;

        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";
        public FloatingJoystick joystick;

        public AudioSource audioSource;
        public AudioClip footstep1;
        public AudioClip footstep2;

        public float footstepInterval = 0.2f;
        private bool isWalking = false;
        private bool isFootstep1 = true;
        private float footstepTimer = 0f;
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

        private void Start()
        {
            // Tell camera to follow transform
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        public void ChangeCameraRotation()
        {
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);
        }

        private void Update()
        {
            HandleCharacterInput();
        }

        private void FixedUpdate()
        {
            if (IsPlayerWalking())
            {
                isWalking = true;
                footstepTimer += Time.deltaTime;

                if (footstepTimer >= footstepInterval)
                {
                    PlayFootstepSound();
                    footstepTimer = 0f;
                }
            }
            else
            {
                isWalking = false;
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }
        }

        private void PlayFootstepSound()
        {
            if (audioSource == null)
                return;

            // Alternate between footstep1 and footstep2
            audioSource.clip = isFootstep1 ? footstep1 : footstep2;
            audioSource.Play();
            isFootstep1 = !isFootstep1;
        }

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            Character.PostInputUpdate(Time.deltaTime, CharacterCamera.transform.forward);
            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = TCKInput.GetAxis("Touchpad").y;
            float mouseLookAxisRight = TCKInput.GetAxis("Touchpad").x;
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Apply inputs to the camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, 0, lookInputVector);
        }

        private void HandleCharacterInput()
        {
            // Build the CharacterInputs struct
#if UNITY_EDITOR
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
#else
            characterInputs.MoveAxisForward = joystick.Vertical;
            characterInputs.MoveAxisRight = joystick.Horizontal;
#endif
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
            characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
            characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }

        private bool IsPlayerWalking()
        {
            return characterInputs.MoveAxisForward != 0 || characterInputs.MoveAxisRight != 0;
        }

        public void MakePlayerJump()
        {
            Character._jumpRequested = true;
        }

        public void StopJump()
        {
            Character._jumpRequested = false;
        }
    }
}
