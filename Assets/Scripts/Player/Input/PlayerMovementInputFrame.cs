using UnityEngine;

// per frame input data
public readonly struct PlayerMovementInputFrame
{
    public PlayerMovementInputFrame(Vector2 move, bool jumpPressed, bool jumpHeld, bool jumpReleased)
    {
        Move = Vector2.ClampMagnitude(move, 1f);
        JumpPressed = jumpPressed;
        JumpHeld = jumpHeld;
        JumpReleased = jumpReleased;
    }

    public static PlayerMovementInputFrame Empty => new PlayerMovementInputFrame(Vector2.zero, false, false, false);

    public Vector2 Move { get; }
    public bool JumpPressed { get; }
    public bool JumpHeld { get; }
    public bool JumpReleased { get; }
}
