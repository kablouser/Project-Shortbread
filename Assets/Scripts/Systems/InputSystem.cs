using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct InputSystem
{
    public Vector3 lastMousePosition;
    public InputDevice usingDevice;
    public bool isInteractDown;
    public bool isEscapeDown;
    public bool isUINavigateDown;

    public void Update(MainScript main)
    {
        Vector3 newMousePosition = Input.mousePosition;

        if (main.playerControls.ActionMap.Aim.IsPressed())
        {
            Vector2 aim = main.playerControls.ActionMap.Aim.ReadValue<Vector2>();
            main.player.SetRotationDegrees(aim);
        }
        else if (Input.mousePresent && Application.isFocused && 0.1f < (newMousePosition - lastMousePosition).sqrMagnitude)
        {
            Vector3 mousePositionWorld = main.mainCamera.ScreenToWorldPoint(newMousePosition);
            main.player.SetRotationDegrees(mousePositionWorld - main.player.transform.position);
        }
        lastMousePosition = newMousePosition;

        main.player.attack.isAttacking = main.playerControls.ActionMap.Shoot.IsPressed();

        if (main.playerControls.ActionMap.Reload.WasPressedThisFrame())
        {
            main.player.attack.Reload(main.attackSystem.player, main.player.statModifiers);
        }

        // dynamically updated button prompts:

        // detect most recently using device
        bool isDeviceChanged = false;
        foreach (var deviceI in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (deviceI != null)
            {
                if (deviceI.noisy || deviceI is Gamepad)
                {
                    foreach (var controlI in deviceI.allControls)
                    {
                        if (controlI.IsActuated(0.05f))
                        {
                            if (usingDevice == null || usingDevice.lastUpdateTime < deviceI.lastUpdateTime)
                            {
                                usingDevice = deviceI;
                                isDeviceChanged = true;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    if (usingDevice == null || usingDevice.lastUpdateTime < deviceI.lastUpdateTime)
                    {
                        usingDevice = deviceI;
                        isDeviceChanged = true;
                    }
                }
            }
        }

        if (isDeviceChanged && usingDevice != null)
        {
            string GetDisplayName(InputControl ic)
            {
                return ic.shortDisplayName == null ?
                    ic.displayName :
                    ic.shortDisplayName;
            }

            foreach (var control in main.playerControls.ActionMap.Interact.controls)
            {
                if (control.device == usingDevice)
                {
                    main.craftingSystem.interactPrompt.text = $"[{GetDisplayName(control)}] Interact";
                    break;
                }
            }

            if (main.shootTutorialText != null)
            {
                foreach (var control in main.playerControls.ActionMap.Shoot.controls)
                {
                    if (control.device == usingDevice)
                    {
                        main.shootTutorialText.text = $"[{GetDisplayName(control)}] Blast";
                        break;
                    }
                }
            }
        }

        isInteractDown = main.playerControls.ActionMap.Interact.WasPressedThisFrame();
        isEscapeDown = main.playerControls.ActionMap.Escape.WasPressedThisFrame();
        isUINavigateDown = main.playerControls.UI.Navigate.WasPressedThisFrame();
        // reselect when navigating and nothing is selected
        if (isUINavigateDown && !main.eventSystem.currentSelectedGameObject && main.eventSystem.firstSelectedGameObject)
        {
            main.eventSystem.SetSelectedGameObject(main.eventSystem.firstSelectedGameObject);
        }
    }

    public void FixedUpdate(MainScript main)
    {
        Vector2 moveDirection = main.playerControls.ActionMap.Move.ReadValue<Vector2>();
        if (1f < moveDirection.sqrMagnitude)
            moveDirection.Normalize();
        main.player.rigidbody.velocity = moveDirection * main.player.moveSpeed;
    }
}
