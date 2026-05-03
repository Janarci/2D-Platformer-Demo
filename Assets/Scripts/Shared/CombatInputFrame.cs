public readonly struct CombatInputFrame
{
    public CombatInputFrame(bool attackPressed, bool attackHeld, bool attackReleased)
    {
        AttackPressed = attackPressed;
        AttackHeld = attackHeld;
        AttackReleased = attackReleased;
    }

    public static CombatInputFrame Empty => new CombatInputFrame(false, false, false);

    public bool AttackPressed { get; }
    public bool AttackHeld { get; }
    public bool AttackReleased { get; }
}
