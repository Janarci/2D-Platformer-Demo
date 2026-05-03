public readonly struct PlayerCombatInputFrame
{
    public PlayerCombatInputFrame(bool attackPressed, bool attackHeld, bool attackReleased)
    {
        AttackPressed = attackPressed;
        AttackHeld = attackHeld;
        AttackReleased = attackReleased;
    }

    public static PlayerCombatInputFrame Empty => new PlayerCombatInputFrame(false, false, false);

    public bool AttackPressed { get; }
    public bool AttackHeld { get; }
    public bool AttackReleased { get; }

    public static implicit operator CombatInputFrame(PlayerCombatInputFrame frame)
    {
        return new CombatInputFrame(frame.AttackPressed, frame.AttackHeld, frame.AttackReleased);
    }
}
