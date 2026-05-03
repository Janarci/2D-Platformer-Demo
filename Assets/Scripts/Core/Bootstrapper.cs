using UnityEngine;

public sealed class Bootstrapper : MonoBehaviour
{
    [Header("Required Systems")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField] private DelayedGameplayEventScheduler delayedGameplayEventScheduler;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private PlatformerContext platformerContext;

    public bool IsInitialized { get; private set; }
    public GameplayEventContext EventContext { get; private set; }

    private void Awake()
    {
        InitializeScene();
    }

    public void InitializeScene()
    {
        if (IsInitialized)
        {
            return;
        }

        ValidateReference(gameStateManager, nameof(gameStateManager));
        ValidateReference(gameplayEventBus, nameof(gameplayEventBus));
        ValidateReference(delayedGameplayEventScheduler, nameof(delayedGameplayEventScheduler));
        ValidateReference(levelManager, nameof(levelManager));
        ValidateReference(platformerContext, nameof(platformerContext));

        EventContext = new GameplayEventContext(
            gameplayEventBus,
            delayedGameplayEventScheduler,
            levelManager,
            platformerContext);

        gameplayEventBus.Initialize(EventContext);
        delayedGameplayEventScheduler.Initialize(gameplayEventBus);
        levelManager.Initialize(platformerContext);
        gameStateManager.Initialize(gameplayEventBus);

        IsInitialized = true;
    }

    private static void ValidateReference(Object reference, string referenceName)
    {
        if (reference == null)
        {
            throw new MissingReferenceException(
                $"{nameof(Bootstrapper)} requires a scene-authored reference for {referenceName}.");
        }
    }
}
