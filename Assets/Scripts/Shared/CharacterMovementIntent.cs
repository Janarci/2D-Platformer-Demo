using UnityEngine;

public struct CharacterMovementIntent
{
    public CharacterMovementIntent(Vector2 move, bool jumpPressed, bool jumpHeld, bool jumpReleased)
    {
        Move = Vector2.ClampMagnitude(move, 1f);
        JumpPressed = jumpPressed;
        JumpHeld = jumpHeld;
        JumpReleased = jumpReleased;
    }

    public Vector2 Move { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpReleased { get; private set; }
}
