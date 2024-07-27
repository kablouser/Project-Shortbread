using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InputSystem
{
    private Vector3 lastMousePosition;

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
    }

    public void FixedUpdate(MainScript main)
    {
        Vector2 moveDirection = main.playerControls.ActionMap.Move.ReadValue<Vector2>();
        if (1f < moveDirection.sqrMagnitude)
            moveDirection.Normalize();
        main.player.rigidbody.velocity = moveDirection * main.player.moveSpeed;
    }
}
