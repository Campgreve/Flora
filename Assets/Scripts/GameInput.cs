using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class GameInput
{
    private static readonly HashSet<string> MissingAxes = new HashSet<string>();

    private static readonly string[] RightStickXAxisCandidates =
    {
        "RightStickHorizontal",
        "RightStickX",
        "JoystickRightHorizontal",
        "Right X",
        "3rd axis",
        "4th axis"
    };

    private static readonly string[] RightStickYAxisCandidates =
    {
        "RightStickVertical",
        "RightStickY",
        "JoystickRightVertical",
        "Right Y",
        "4th axis",
        "5th axis"
    };

    private static readonly string[] TriggerCandidates =
    {
        "RightTrigger",
        "R2",
        "6th axis",
        "7th axis",
        "9th axis",
        "10th axis",
        "3rd axis"
    };

    public static Vector2 GetMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.0001f)
            {
                return stick;
            }
        }
#endif
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public static bool TryGetRightStick(out Vector2 rightStick, float deadZone = 0.2f)
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            rightStick = pad.rightStick.ReadValue();
            if (rightStick.sqrMagnitude >= deadZone * deadZone)
            {
                return true;
            }
        }
#endif

        float x = GetAxisFirstAvailable(RightStickXAxisCandidates);
        float y = GetAxisFirstAvailable(RightStickYAxisCandidates);
        rightStick = new Vector2(x, y);
        return rightStick.sqrMagnitude >= deadZone * deadZone;
    }

    public static bool TryGetAimDirection(Vector3 worldOrigin, out Vector2 aimDir, float deadZone = 0.2f)
    {
        if (TryGetRightStick(out Vector2 stick, deadZone))
        {
            aimDir = stick.normalized;
            return true;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            aimDir = Vector2.down;
            return false;
        }

        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector2 dir = (mousePos - worldOrigin);
        aimDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.down;
        return false;
    }

    public static bool IsShootHeld()
    {
        if (Input.GetMouseButton(0))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.rightTrigger.ReadValue() > 0.2f)
        {
            return true;
        }
#endif

        // Common gamepad mappings for R2 / RT across platforms.
        if (Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.JoystickButton7))
        {
            return true;
        }

        return GetAxisAbsFirstAvailable(TriggerCandidates) > 0.45f;
    }

    public static bool IsConfirmPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return Input.GetKeyDown(KeyCode.E) ||
               Input.GetKeyDown(KeyCode.Return) ||
               Input.GetKeyDown(KeyCode.JoystickButton0); // X / A
    }

    public static bool IsDashPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonWest.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.JoystickButton2); // Square / X
    }

    public static bool IsCancelPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonEast.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return Input.GetKeyDown(KeyCode.Escape) ||
               Input.GetKeyDown(KeyCode.Backspace) ||
               Input.GetKeyDown(KeyCode.JoystickButton1); // O / B
    }

    public static bool IsPausePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.startButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return Input.GetKeyDown(KeyCode.Escape) ||
               Input.GetKeyDown(KeyCode.JoystickButton7) || // Start / Options (common)
               Input.GetKeyDown(KeyCode.JoystickButton9);
    }

    public static bool IsStatsPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonNorth.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return Input.GetKeyDown(KeyCode.Tab) ||
               Input.GetKeyDown(KeyCode.JoystickButton3); // Triangle / Y
    }

    private static float GetAxisFirstAvailable(string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            if (TryGetAxis(candidates[i], out float value))
            {
                return value;
            }
        }

        return 0f;
    }

    private static float GetAxisAbsFirstAvailable(string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            if (TryGetAxis(candidates[i], out float value))
            {
                return Mathf.Abs(value);
            }
        }

        return 0f;
    }

    private static bool TryGetAxis(string axisName, out float value)
    {
        if (string.IsNullOrWhiteSpace(axisName) || MissingAxes.Contains(axisName))
        {
            value = 0f;
            return false;
        }

        try
        {
            value = Input.GetAxisRaw(axisName);
            return true;
        }
        catch (ArgumentException)
        {
            MissingAxes.Add(axisName);
            value = 0f;
            return false;
        }
    }
}
