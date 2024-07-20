using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputSystem
{
    public const string
        HORIZONTAL = "Horizontal",
        VERTICAL = "Vertical",
        FIRE1 = "Fire1";

    public static void Update(MainScript main)
    {
        main.player.attack.isAttacking = Input.GetButton(FIRE1);
    }

    public static void FixedUpdate(MainScript main)
    {
        main.player.rigidbody.velocity = new(Input.GetAxis(HORIZONTAL), Input.GetAxis(VERTICAL));
    }
}
