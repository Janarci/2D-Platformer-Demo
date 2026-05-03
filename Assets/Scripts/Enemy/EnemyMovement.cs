using UnityEngine;

public sealed class EnemyMovement : CharacterMovement
{
    public void SetHorizontalMove(float horizontal)
    {
        SetIntent(new CharacterMovementIntent(new Vector2(horizontal, 0f), false, false, false));
    }

    public void StopHorizontalMove()
    {
        SetHorizontalMove(0f);
    }

    public void FaceDirection(float horizontalDirection)
    {
        SetFacingDirection(horizontalDirection);
    }

    public bool IsFacingPosition(Vector2 position)
    {
        var directionToPosition = position.x - transform.position.x;
        return Mathf.Abs(directionToPosition) <= 0.01f
            || Mathf.Sign(directionToPosition) == Mathf.Sign(FacingDirection);
    }
}
