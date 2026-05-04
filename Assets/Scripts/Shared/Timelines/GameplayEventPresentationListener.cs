using UniRx;
using UnityEngine;

public sealed class GameplayEventPresentationListener : MonoBehaviour
{
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private VFXSpawner vfxSpawner;
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private CombatComponent combatComponent;
    [SerializeField] private CharacterStateMachine characterStateMachine;

    private readonly CompositeDisposable subscriptions = new();
    private bool isObserving;

    private void Awake()
    {
        ResolveReferences();
        BindGameplayEvents();
    }

    private void Start()
    {
        BindGameplayEvents();
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus;
        BindGameplayEvents();
    }

    private void ResolveReferences()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        if (!audioPlayer)
        {
            audioPlayer = GetComponent<AudioPlayer>();
        }

        if (!vfxSpawner)
        {
            vfxSpawner = GetComponent<VFXSpawner>();
        }

        if (!healthComponent)
        {
            healthComponent = ResolveTargetComponent<HealthComponent>();
        }

        if (!combatComponent)
        {
            combatComponent = ResolveTargetComponent<CombatComponent>();
        }
        
        if (!characterStateMachine)
        {
            characterStateMachine = ResolveTargetComponent<CharacterStateMachine>();
        }
        
        if (!gameplayEventBus)
        {
            gameplayEventBus = FindObjectOfType<GameplayEventBus>(true);
        }
    }

    private void BindGameplayEvents()
    {
        if (isObserving || gameplayEventBus == null || !gameplayEventBus.IsInitialized || targetObject == null)
        {
            return;
        }

        isObserving = true;

        gameplayEventBus
            .ObserveTarget<PlaySfxGameplayEvent>(targetObject)
            .Subscribe(HandlePlaySfx)
            .AddTo(subscriptions);

        gameplayEventBus
            .ObserveTarget<SpawnVfxGameplayEvent>(targetObject)
            .Subscribe(HandleSpawnVfx)
            .AddTo(subscriptions);

        gameplayEventBus
            .ObserveTarget<DamageGameplayEvent>(targetObject)
            .Subscribe(HandleDamage)
            .AddTo(subscriptions);
    }

    private void HandlePlaySfx(PlaySfxGameplayEvent gameplayEvent)
    {
        audioPlayer?.PlayOneShot(gameplayEvent.Clip, gameplayEvent.Volume);
    }

    private void HandleSpawnVfx(SpawnVfxGameplayEvent gameplayEvent)
    {
        if (vfxSpawner == null || gameplayEvent.Prefab == null)
        {
            return;
        }

        var spawnPoint = gameplayEvent.SpawnPoint != null ? gameplayEvent.SpawnPoint : transform;
        var parent = ResolveVfxParent(gameplayEvent, spawnPoint);
        vfxSpawner.Spawn(
            gameplayEvent.Prefab,
            spawnPoint.position + gameplayEvent.Offset,
            spawnPoint.rotation,
            parent);
    }

    private Transform ResolveVfxParent(SpawnVfxGameplayEvent gameplayEvent, Transform spawnPoint)
    {
        switch (gameplayEvent.ParentMode)
        {
            case GameplayEventVfxParentMode.SpawnPoint:
                return spawnPoint;

            case GameplayEventVfxParentMode.Target:
                return targetObject != null ? targetObject.transform : transform;

            case GameplayEventVfxParentMode.None:
            default:
                return null;
        }
    }

    private void HandleDamage(DamageGameplayEvent gameplayEvent)
    {
        if (healthComponent == null
            || !healthComponent.ApplyDamage(
                gameplayEvent.Amount,
                gameplayEvent.Instigator,
                gameplayEvent.AllowFriendlyFire))
        {
            return;
        }

        combatComponent?.ApplyDamageFeedback(gameplayEvent);
        
        if (healthComponent.IsAlive)
        {
            characterStateMachine?.RequestHitReact(gameplayEvent.Instigator);
        }
    }

    private T ResolveTargetComponent<T>() where T : Component
    {
        if (targetObject != null)
        {
            var targetComponent = targetObject.GetComponent<T>();
            if (targetComponent != null)
            {
                return targetComponent;
            }

            return targetObject.GetComponentInChildren<T>(true);
        }

        return GetComponent<T>();
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
        gameplayEventBus.ReleaseTarget(gameObject);
    }
}
