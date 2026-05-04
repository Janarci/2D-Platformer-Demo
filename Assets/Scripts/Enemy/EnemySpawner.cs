using UniRx;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField] private DelayedGameplayEventScheduler delayedGameplayEventScheduler;
    [SerializeField] private PlatformerContext platformerContext;
    [SerializeField] private Transform spawnPoint;
    
    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool parentEnemyToSpawner;
    [SerializeField, Min(0f)] private float spawnDelay = 5f;

    private readonly CompositeDisposable subscriptions = new();
    // spawner only tracks/spawns 1
    private GameObject currentEnemy;
    private bool isRespawnedQueued;
    private bool isObservingGameplayEvents;

    public GameObject CurrentEnemy => currentEnemy;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (!gameplayEventBus)
        {
            gameplayEventBus = FindObjectOfType<GameplayEventBus>(true);
        }

        if (!delayedGameplayEventScheduler)
        {
            delayedGameplayEventScheduler = FindObjectOfType<DelayedGameplayEventScheduler>(true);
        }
    }

    public void Initialize(GameplayEventBus eventBus, PlatformerContext context = null)
    {
        gameplayEventBus = eventBus;

        if (context != null)
        {
            platformerContext = context;
        }

        BindGameplayEvents();
        InitializeSpawnedEnemy(currentEnemy);
    }

    private void BindGameplayEvents()
    {
        if (isObservingGameplayEvents || gameplayEventBus == null)
        {
            return;
        }

        isObservingGameplayEvents = true;

        gameplayEventBus
            .Observe<EnemyDestroyedGameplayEvent>()
            .Where(IsCurrentEnemyDestroyedEvent)
            .Subscribe(_ => ResetEnemy())
            .AddTo(subscriptions);
    }
    
    private void Start()
    {
        if (spawnOnStart && currentEnemy == null)
        {
            SpawnEnemy();
        }
        BindGameplayEvents();
        
    }
    public GameObject SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} requires an enemy prefab.", this);
            return null;
        }

        var spawnTransform = spawnPoint != null ? spawnPoint : transform;
        var parent = parentEnemyToSpawner ? transform : null;

        currentEnemy = Instantiate(
            enemyPrefab,
            spawnTransform.position,
            spawnTransform.rotation,
            parent);

        InitializeSpawnedEnemy(currentEnemy);
        return currentEnemy;
    }

    public void ResetEnemy()
    {
        
        //todo: reuse enemy
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
            currentEnemy = null;
        }

        SpawnEnemy();
    }

    private bool IsCurrentEnemyDestroyedEvent(EnemyDestroyedGameplayEvent gameplayEvent)
    {
        if (gameplayEvent == null || currentEnemy == null)
        {
            return false;
        }

        return IsCurrentEnemyObject(gameplayEvent.Enemy)
            || IsCurrentEnemyObject(
                gameplayEvent.HealthComponent != null
                    ? gameplayEvent.HealthComponent.gameObject
                    : null);
    }

    private bool IsCurrentEnemyObject(GameObject candidate)
    {
        return candidate != null
            && currentEnemy != null
            && (candidate == currentEnemy);
    }

    private void InitializeSpawnedEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (gameplayEventBus != null)
        {
            foreach (var lifecycleController in enemy.GetComponentsInChildren<EnemyLifecycleController>(true))
            {
                lifecycleController.Initialize(gameplayEventBus);
            }

            foreach (var stateMachine in enemy.GetComponentsInChildren<CharacterStateMachine>(true))
            {
                stateMachine.Initialize(gameplayEventBus);
            }

            foreach (var timelineAnimator in enemy.GetComponentsInChildren<CharacterTimelineAnimator>(true))
            {
                timelineAnimator.Initialize(gameplayEventBus);
            }

            foreach (var combatComponent in enemy.GetComponentsInChildren<CombatComponent>(true))
            {
                combatComponent.Initialize(gameplayEventBus);
            }
        }

        if (platformerContext == null)
        {
            return;
        }

        foreach (var movement in enemy.GetComponentsInChildren<CharacterMovement>(true))
        {
            movement.Initialize(platformerContext);
        }
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
    }
}
