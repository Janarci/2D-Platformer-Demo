using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class InputSystemPlayerInputSource : MonoBehaviour, IPlayerInputSource
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private bool enableActionsOnEnable = true;
#endif

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (!enableActionsOnEnable)
        {
            return;
        }

        moveAction?.action?.Enable();
        jumpAction?.action?.Enable();
        attackAction?.action?.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (!enableActionsOnEnable)
        {
            return;
        }

        moveAction?.action?.Disable();
        jumpAction?.action?.Disable();
        attackAction?.action?.Disable();
#endif
    }

    public PlayerMovementInputFrame ReadMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        return new PlayerMovementInputFrame(
            ReadMove(),
            ReadJumpPressed(),
            ReadJumpHeld(),
            ReadJumpReleased());
#else
        return PlayerMovementInputFrame.Empty;
#endif
    }

    public PlayerCombatInputFrame ReadCombatInput()
    {
#if ENABLE_INPUT_SYSTEM
        return new PlayerCombatInputFrame(
            ReadAttackPressed(),
            ReadAttackHeld(),
            ReadAttackReleased());
#else
        return PlayerCombatInputFrame.Empty;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private Vector2 ReadMove()
    {
        if (moveAction && moveAction.action != null)
        {
            return Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);
        }

        var move = Vector2.zero;

        if (Keyboard.current != null)
        {
            move.x += Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? -1f : 0f;
            move.x += Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f;
        }

        if (Gamepad.current != null)
        {
            move += Gamepad.current.leftStick.ReadValue();
        }

        return Vector2.ClampMagnitude(move, 1f);
    }

    private bool ReadJumpPressed()
    {
        if (jumpAction && jumpAction.action != null)
        {
            return jumpAction.action.WasPressedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }

    private bool ReadJumpHeld()
    {
        if (jumpAction&& jumpAction.action != null)
        {
            return jumpAction.action.IsPressed();
        }

        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed
            || Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
    }

    private bool ReadJumpReleased()
    {
        if (jumpAction && jumpAction.action != null)
        {
            return jumpAction.action.WasReleasedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame;
    }

    private bool ReadAttackPressed()
    {
        if (attackAction && attackAction.action != null)
        {
            return attackAction.action.WasPressedThisFrame();
        }

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool ReadAttackHeld()
    {
        if (attackAction && attackAction.action != null)
        {
            return attackAction.action.IsPressed();
        }

        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool ReadAttackReleased()
    {
        if (attackAction && attackAction.action != null)
        {
            return attackAction.action.WasReleasedThisFrame();
        }

        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }
#endif
}
