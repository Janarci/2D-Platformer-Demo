public sealed class GameplayEventContext
{
    public GameplayEventContext(
        GameplayEventBus eventBus,
        DelayedGameplayEventScheduler delayedScheduler,
        LevelManager levelManager,
        PlatformerContext platformerContext)
    {
        EventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
        DelayedScheduler = delayedScheduler ?? throw new System.ArgumentNullException(nameof(delayedScheduler));
        LevelManager = levelManager ?? throw new System.ArgumentNullException(nameof(levelManager));
        PlatformerContext = platformerContext ?? throw new System.ArgumentNullException(nameof(platformerContext));
    }

    public GameplayEventBus EventBus { get; }
    public DelayedGameplayEventScheduler DelayedScheduler { get; }
    public LevelManager LevelManager { get; }
    public PlatformerContext PlatformerContext { get; }
}
