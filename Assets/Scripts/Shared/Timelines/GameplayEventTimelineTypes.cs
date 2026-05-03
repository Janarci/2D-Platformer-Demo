public enum GameplayEventTimelineType
{
    Damage,
    PlaySfx,
    SpawnVfx,
    AttackTimelineFinished,
    EnemyDestroyed,
    EnemyRevive,
    EnemyRevived
}

public enum GameplayEventTargetRole
{
    Self,
    None
}

public enum GameplayEventVfxParentMode
{
    None,
    SpawnPoint,
    Target
}
