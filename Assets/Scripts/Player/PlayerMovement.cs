public sealed class PlayerMovement : CharacterMovement
{
    public void SetInput(PlayerMovementInputFrame movementInputFrame)
    {
        SetIntent(new CharacterMovementIntent(
            movementInputFrame.Move,
            movementInputFrame.JumpPressed,
            movementInputFrame.JumpHeld,
            movementInputFrame.JumpReleased));
    }
}
