using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine;
using SimplePlatformer.Player;
using SimplePlatformer.Assets.Scripts.Player.Input;

namespace SimplePlatformer.Assets.Scripts.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputHandler : MonoBehaviour
    {
        private PlayerInput _playerInput;

        // State
        private InputState CurrentInput { get; set; }
        // Actions
        public Action OnAttack { get; set; }
        public Action OnInteract { get; set; }
        public Action OnJumpStarted { get; set; }
        public Action OnJumpPerformed { get; set; }
        public Action OnBowAttack { get; set; }
        public Action OnTogglePause { get; set; }

        public FixedJoystick fixedJoystick;

        public bool androidMode = false;

        private string currentControlScheme = "PlayerControlls";

        private string actionMapPlayerControls = "PlayerControlls";
        private string actionMapMenuControls = "UI";

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            CurrentInput = new();
            CurrentInput.CanJump = true;
            CurrentInput.CanAttack = true;
            CurrentInput.CanInteract = true;
        }

        private void Update()
        {
            if (fixedJoystick != null && androidMode) { 
                CurrentInput.MovementDirection = fixedJoystick.Direction;
            }
        }

        public InputState GetInputState()
        {
            // TODO: Add middlewares...
            // GameManager system

            if (GameManager.GetInstance().PlayerIsDead) {
                //CurrentInput.CanInteract = false;
                //CurrentInput.CanJump = false;
                //CurrentInput.CanAttack = false;
                //CurrentInput.MovementDirection = Vector2.zero;
            }

            return CurrentInput;
        }

        //INPUT SYSTEM ACTION METHODS --------------

        //This is called from PlayerInput; when a joystick or arrow keys has been pushed.
        //It stores the input Vector as a Vector3 to then be used by the smoothing function.

        public void Movement(InputAction.CallbackContext value)
        {
            Vector2 inputMovement = value.ReadValue<Vector2>();
            CurrentInput.MovementDirection = inputMovement;
            //rawInputMovement = new Vector2(inputMovement.x, rb2d.velocity.y);
        }

        //This is called from PlayerInput, when a button has been pushed, that corresponds with the 'Attack' action
        public void Attack(InputAction.CallbackContext value)
        {
            if (value.started && GetInputState().CanAttack)
            {
                OnAttack?.Invoke();
                //playerCombatBehaviour.Attack();
            }
        }

        //This has a hold Interaction. When the hold ends, starts falling
        public void Jump(InputAction.CallbackContext value)
        {
            if (value.started)
            {
                OnJumpStarted?.Invoke();
                //CurrentInput.IsJumping = true;
            }
            if (value.performed || value.canceled)
            {
                OnJumpPerformed?.Invoke();
                //CurrentInput.IsJumping = false;
                //playerMovementBehaviour.CancelJumping();
            }

        }

        public void Interact(InputAction.CallbackContext value)
        {
            if (value.started && GetInputState().CanInteract)
            {
                OnInteract?.Invoke();
                //playerInteractableBehaviour.Interact();
            }
        }

        public void BowAttack(InputAction.CallbackContext value)
        {
            if (value.started)
            {
                OnBowAttack?.Invoke();
                //playerCombatBehaviour.BowAttack();
            }
        }


        //This is called from Player Input, when a button has been pushed, that correspons with the 'TogglePause' action
        public void TogglePause(InputAction.CallbackContext value)
        {
            if (value.started)
            {
                OnTogglePause?.Invoke();
                GameManager.GetInstance().TogglePauseState();
            }
        }

        //INPUT SYSTEM AUTOMATIC CALLBACKS --------------

        //This is automatically called from PlayerInput, when the input device has changed
        //(IE: Keyboard -> Xbox Controller)
        public void ControlsChanged()
        {

            if (_playerInput.currentControlScheme != currentControlScheme)
            {

                currentControlScheme = _playerInput.currentControlScheme;

                //playerVisualsBehaviour.UpdatePlayerVisuals();
                RemoveAllBindingOverrides();
            }
        }

        //This is automatically called from PlayerInput, when the input device has been disconnected and can not be identified
        //IE: Device unplugged or has run out of batteries

        public void DeviceLost()
        {
            currentControlScheme = _playerInput.currentControlScheme;
            //playerVisualsBehaviour.SetDisconnectedDeviceVisuals();
        }


        public void OnDeviceRegained()
        {
            StartCoroutine(WaitForDeviceToBeRegained());
        }

        IEnumerator WaitForDeviceToBeRegained()
        {
            yield return new WaitForSeconds(0.1f);
            //playerVisualsBehaviour.UpdatePlayerVisuals();
        }

        void RemoveAllBindingOverrides()
        {
            InputActionRebindingExtensions.RemoveAllBindingOverrides(_playerInput.currentActionMap);
        }

        //Switching Action Maps ----

        public void EnableGameplayControls()
        {
            //_playerInput.SwitchCurrentActionMap(actionMapPlayerControls);
        }

        public void EnablePauseMenuControls()
        {
            _playerInput.SwitchCurrentActionMap(actionMapMenuControls);
        }

        private void OnEnable()
        {
            _playerInput.actions["Movement"].performed += Movement;
            _playerInput.actions["Movement"].canceled += Movement;
            _playerInput.actions["Attack"].performed += Attack;
            _playerInput.actions["Attack"].started += Attack;
            _playerInput.actions["Jump"].performed += Jump;
            _playerInput.actions["Jump"].started += Jump;
            _playerInput.actions["Jump"].canceled += Jump;
            _playerInput.actions["Interact"].started += Interact;
            _playerInput.actions["Pause"].started += TogglePause;
            _playerInput.actions["Bow"].performed += BowAttack;
        }

        private void OnDisable()
        {
            _playerInput.actions["Movement"].performed -= Movement;
            _playerInput.actions["Movement"].canceled -= Movement;
            _playerInput.actions["Attack"].performed -= Attack;
            _playerInput.actions["Attack"].started -= Attack;
            _playerInput.actions["Jump"].performed -= Jump;
            _playerInput.actions["Jump"].started -= Jump;
            _playerInput.actions["Jump"].canceled -= Jump;
            _playerInput.actions["Interact"].started -= Interact;
            _playerInput.actions["Pause"].started -= TogglePause;
            _playerInput.actions["Bow"].performed -= BowAttack;
        }
    }
}
