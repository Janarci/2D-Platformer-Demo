public interface IPlayerInputSource
{
    PlayerMovementInputFrame ReadMovementInput();
    PlayerCombatInputFrame ReadCombatInput();
}
