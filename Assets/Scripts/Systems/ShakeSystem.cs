using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
// Both screen shake and controller rumble
public struct ShakeSystem
{
    [Header("Do touch")]
    public float magnitude;
    public float fadeRate;
    public float cameraShakeMaxAcceleration;
    public float cameraShakeLerpRate;
    public float cameraShakeDistanceScale;

    public float gamepadLowFrequency;
    public float gamepadHighFrequency;

    [Header("DON'T touch")]
    public Vector3 cameraPositionWithoutShake;
    public Vector2 cameraShakeOffset;
    public Vector2 cameraShakeLastVelocity;

    // rumble doesn't fade away automatically, need to clean it up
    private Gamepad lastGamepad;

    /* amount is 0-1 */
    public void Shake(float amount = 1f)
    {
        magnitude = amount * 20f;
    }

    public void Start(MainScript script)
    {
        cameraPositionWithoutShake = script.mainCamera.transform.position;
    }

    public void UpdateEvenWhenPaused(MainScript mainScript, float deltaTime)
    {
        magnitude -= fadeRate * deltaTime;
        magnitude = Mathf.Max(0f, magnitude);

        // camera shake
        {
            Vector2 randomTarget = 0f < magnitude ?
                cameraShakeDistanceScale * magnitude * Random.insideUnitCircle :
                Vector2.zero;

            Vector2 pureVelocity = randomTarget - cameraShakeOffset;
            Vector2 acceleration = pureVelocity - cameraShakeLastVelocity;
            float accelMag = acceleration.magnitude * deltaTime;
            if (cameraShakeMaxAcceleration < accelMag)
            {
                acceleration *= cameraShakeMaxAcceleration / accelMag;
            }
            cameraShakeLastVelocity += acceleration;

            cameraShakeOffset += Vector2.Lerp(Vector2.zero, cameraShakeLastVelocity, cameraShakeLerpRate * deltaTime);

            cameraPositionWithoutShake = Vector3.Lerp(cameraPositionWithoutShake, mainScript.player.transform.position, deltaTime * 5f);
            mainScript.mainCamera.transform.position = cameraPositionWithoutShake + (Vector3)cameraShakeOffset;
        }

        // controller rumble
        {
            Gamepad gamepad = mainScript.inputSystem.usingDevice as Gamepad;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(gamepadLowFrequency * magnitude / 20f, gamepadHighFrequency * magnitude / 20);
            }

            if (lastGamepad != gamepad && lastGamepad != null)
            {
                // gamepad changed, need to clear the rumble
                lastGamepad.SetMotorSpeeds(0f, 0f);
            }
            lastGamepad = gamepad;
        }
    }

    public void OnDestroy()
    {
        if (lastGamepad != null)
        {
            lastGamepad.SetMotorSpeeds(0f, 0f);
            lastGamepad = null;
        }
    }
}
