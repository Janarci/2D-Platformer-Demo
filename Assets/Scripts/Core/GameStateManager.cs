using System.Collections;
using UniRx;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameStateManager : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private HealthComponent playerHealth;

    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreenRoot;
    [SerializeField] private TMP_Text deathMessageText;
    [SerializeField] private Button restartButton;
    [SerializeField] private TMP_Text restartButtonLabel;
    [SerializeField] private bool pauseTimeOnPlayerDeath = true;
    [SerializeField] private string deathMessage = "You Died";
    [SerializeField] private string restartButtonText = "Restart Game";

    [Header("Slime Kills")]
    [SerializeField] private TMP_Text slimeKillsText;
    [SerializeField] private string slimeKillsTextFormat = "Slimes killed: {0}";
    [SerializeField] private EntityMetadata slimeMetadata;

    private GameplayEventBus gameplayEventBus;
    private readonly CompositeDisposable subscriptions = new();
    private bool isObservingPlayerHealthEvents;
    private bool isObservingSlimeDeathEvents;
    private PlatformerContext platformerContext;
    private bool isDeathScreenConfigured;
    private bool hasPausedTimeForPlayerDeath;
    private float timeScaleBeforePlayerDeath = 1f;
    private int slimeKillCount;
    
    private int activeHitstopRequests;
    private float timeScaleBeforeHitstop = 1f;

    public static GameStateManager Current { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsPlayerDead { get; private set; }
    public int SlimeKillCount => slimeKillCount;
    public GameplayEventBus EventBus => gameplayEventBus;
    public GameObject PlayerObject
    {
        get
        {
            ResolvePlayerReferences();
            InitializePlayerHealth();
            return playerObject;
        }
    }

    public HealthComponent PlayerHealth
    {
        get
        {
            ResolvePlayerReferences();
            InitializePlayerHealth();
            return playerHealth;
        }
    }

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning(
                $"Multiple {nameof(GameStateManager)} instances exist. {nameof(GameStateManager)}.{nameof(Current)} will keep using the first active instance.",
                this);
            return;
        }
        ResetHitstop();

        Current = this;
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        Initialize(eventBus, null);
    }

    public void Initialize(GameplayEventBus eventBus, PlatformerContext context)
    {
        gameplayEventBus = eventBus != null
            ? eventBus
            : throw new MissingReferenceException($"{nameof(GameStateManager)} requires a {nameof(GameplayEventBus)}.");

        if (context != null)
        {
            platformerContext = context;
        }

        InitializeHealthComponents();
        InitializeCharacterMovement();
        ResolvePlayerReferences();
        InitializePlayerHealth();
        ConfigureDeathScreen();
        HideDeathScreen();
        BindPlayerHealthEvents();
        BindSlimeDeathEvents();
        RenderSlimeKillCount();
        IsInitialized = true;
    }

    private void Start()
    {
        if (!IsInitialized)
        {
            return;
        }

        ResolvePlayerReferences();
        InitializePlayerHealth();
        ConfigureDeathScreen();
        HideDeathScreen();
        BindPlayerHealthEvents();
        BindSlimeDeathEvents();
        RenderSlimeKillCount();
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        gameplayEventBus.Tick(Time.deltaTime);
    }

    public bool TryFireTimelineClip(GameplayEventTimelineClip clip, GameObject actorRoot)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} received a null timeline clip.", this);
            return false;
        }

        if (actorRoot == null)
        {
            Debug.LogWarning(
                $"{nameof(GameStateManager)} cannot fire {clip.EventType} because the timeline track is not bound to an actor {nameof(GameObject)}.",
                this);
            return false;
        }

        if (gameplayEventBus == null || !gameplayEventBus.IsInitialized)
        {
            Debug.LogWarning(
                $"{nameof(GameStateManager)} cannot fire {clip.EventType} because the {nameof(GameplayEventBus)} is missing or uninitialized.",
                this);
            return false;
        }

        if (!TryBuildTimelineGameplayEvent(clip, actorRoot, out var gameplayEvent))
        {
            return false;
        }

        gameplayEventBus.FireInstant(gameplayEvent);
        return true;
    }

    private void BindPlayerHealthEvents()
    {
        if (isObservingPlayerHealthEvents || gameplayEventBus == null || !gameplayEventBus.IsInitialized)
        {
            return;
        }

        isObservingPlayerHealthEvents = true;

        gameplayEventBus
            .Observe<HealthDamagedGameplayEvent>()
            .Where(IsOwnPlayerHealthEvent)
            .Subscribe(HandlePlayerHealthDamagedEvent)
            .AddTo(subscriptions);

        gameplayEventBus
            .Observe<HealthUpdatedGameplayEvent>()
            .Where(IsOwnPlayerHealthEvent)
            .Subscribe(HandlePlayerHealthUpdatedEvent)
            .AddTo(subscriptions);

        gameplayEventBus
            .Observe<HealthDiedGameplayEvent>()
            .Where(IsOwnPlayerHealthEvent)
            .Subscribe(HandlePlayerHealthDiedEvent)
            .AddTo(subscriptions);

        gameplayEventBus
            .Observe<PlayerDiedEvent>()
            .Where(IsOwnPlayerDiedEvent)
            .Subscribe(HandlePlayerDiedEvent)
            .AddTo(subscriptions);
    }

    private void BindSlimeDeathEvents()
    {
        if (isObservingSlimeDeathEvents || gameplayEventBus == null || !gameplayEventBus.IsInitialized)
        {
            return;
        }

        isObservingSlimeDeathEvents = true;

        gameplayEventBus
            .Observe<HealthDiedGameplayEvent>()
            .Where(IsSlimeDeadEvent)
            .Subscribe(HandleSlimeDiedEvent)
            .AddTo(subscriptions);
    }

    private void HandlePlayerHealthDamagedEvent(HealthDamagedGameplayEvent gameplayEvent)
    {
        if (!CanPublishPlayerHealthEvent(gameplayEvent.HealthComponent))
        {
            return;
        }

        gameplayEventBus.FireInstant(
            new PlayerDamagedEvent(
                gameplayEvent.Instigator,
                PlayerObject,
                gameplayEvent.HealthComponent,
                gameplayEvent.Damage,
                gameplayEvent.CurrentHealth,
                gameplayEvent.MaxHealth));
    }

    private void HandlePlayerHealthUpdatedEvent(HealthUpdatedGameplayEvent gameplayEvent)
    {
        if (!CanPublishPlayerHealthEvent(gameplayEvent.HealthComponent))
        {
            return;
        }

        if (gameplayEvent.CurrentHealth > 0)
        {
            IsPlayerDead = false;
            HidePlayerDeathScreen();
        }

        gameplayEventBus.FireInstant(
            new PlayerHealthUpdatedEvent(
                gameplayEvent.Instigator,
                PlayerObject,
                gameplayEvent.HealthComponent,
                gameplayEvent.PreviousHealth,
                gameplayEvent.CurrentHealth,
                gameplayEvent.MaxHealth));
    }

    private void HandlePlayerHealthDiedEvent(HealthDiedGameplayEvent gameplayEvent)
    {
        if (!CanPublishPlayerHealthEvent(gameplayEvent.HealthComponent) || IsPlayerDead)
        {
            return;
        }

        gameplayEventBus.FireInstant(
            new PlayerDiedEvent(
                gameplayEvent.Instigator,
                PlayerObject,
                gameplayEvent.HealthComponent));
    }

    private void HandlePlayerDiedEvent(PlayerDiedEvent gameplayEvent)
    {
        if (IsPlayerDead)
        {
            return;
        }

        IsPlayerDead = true;
        ShowPlayerDeathScreen();
        Debug.Log($"{nameof(GameStateManager)} handled player death.", this);
    }

    private void HandleSlimeDiedEvent(HealthDiedGameplayEvent gameplayEvent)
    {
        slimeKillCount++;
        RenderSlimeKillCount();
    }

    private void RenderSlimeKillCount()
    {
        if (slimeKillsText != null)
        {
            slimeKillsText.text = string.Format(slimeKillsTextFormat, slimeKillCount);
        }
    }

    private bool IsSlimeDeadEvent(HealthDiedGameplayEvent gameplayEvent)
    {
        if (gameplayEvent == null || slimeMetadata == null)
        {
            return false;
        }

        var metadata = ResolveEntityMetadata(gameplayEvent.HealthComponent.gameObject);
        return metadata == slimeMetadata;
    }

    private void ConfigureDeathScreen()
    {
        if (isDeathScreenConfigured)
        {
            return;
        }

        if (deathMessageText != null)
        {
            deathMessageText.text = deathMessage;
        }

        if (restartButtonLabel != null)
        {
            restartButtonLabel.text = restartButtonText;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartCurrentScene);
            restartButton.onClick.AddListener(RestartCurrentScene);
        }

        isDeathScreenConfigured = true;
    }

    private void ShowPlayerDeathScreen()
    {
        ConfigureDeathScreen();

        if (pauseTimeOnPlayerDeath && !hasPausedTimeForPlayerDeath)
        {
            timeScaleBeforePlayerDeath = Time.timeScale;
            Time.timeScale = 0f;
            hasPausedTimeForPlayerDeath = true;
        }

        if (deathScreenRoot != null)
        {
            deathScreenRoot.SetActive(true);
        }
    }

    private void HidePlayerDeathScreen()
    {
        HideDeathScreen();

        if (hasPausedTimeForPlayerDeath)
        {
            Time.timeScale = timeScaleBeforePlayerDeath;
            hasPausedTimeForPlayerDeath = false;
        }
    }

    private void HideDeathScreen()
    {
        if (deathScreenRoot != null)
        {
            deathScreenRoot.SetActive(false);
        }
    }

    private void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        hasPausedTimeForPlayerDeath = false;
        IsPlayerDead = false;

        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private bool IsOwnPlayerDiedEvent(PlayerDiedEvent gameplayEvent)
    {
        if (gameplayEvent == null)
        {
            return false;
        }

        ResolvePlayerReferences();

        return gameplayEvent.HealthComponent == playerHealth
            || gameplayEvent.Player == playerObject
            || (playerHealth != null && gameplayEvent.Player == playerHealth.gameObject);
    }

    private bool CanPublishPlayerHealthEvent(HealthComponent healthComponent)
    {
        return gameplayEventBus != null
            && gameplayEventBus.IsInitialized
            && healthComponent != null
            && healthComponent == playerHealth;
    }

    private bool IsOwnPlayerHealthEvent(HealthDamagedGameplayEvent gameplayEvent)
    {
        return IsOwnPlayerHealth(gameplayEvent != null ? gameplayEvent.HealthComponent : null);
    }

    private bool IsOwnPlayerHealthEvent(HealthUpdatedGameplayEvent gameplayEvent)
    {
        return IsOwnPlayerHealth(gameplayEvent != null ? gameplayEvent.HealthComponent : null);
    }

    private bool IsOwnPlayerHealthEvent(HealthDiedGameplayEvent gameplayEvent)
    {
        return IsOwnPlayerHealth(gameplayEvent != null ? gameplayEvent.HealthComponent : null);
    }

    private bool IsOwnPlayerHealth(HealthComponent healthComponent)
    {
        if (healthComponent == null)
        {
            return false;
        }

        if (playerHealth == null)
        {
            ResolvePlayerReferences();
        }

        return healthComponent == playerHealth;
    }

    private void InitializePlayerHealth()
    {
        if (playerHealth != null && gameplayEventBus != null)
        {
            playerHealth.Initialize(gameplayEventBus);
        }
    }

    private void InitializeHealthComponents()
    {
        if (gameplayEventBus == null)
        {
            return;
        }

        var healthComponents = FindObjectsOfType<HealthComponent>(true);
        foreach (var healthComponent in healthComponents)
        {
            healthComponent.Initialize(gameplayEventBus);
        }
    }

    private void InitializeCharacterMovement()
    {
        if (platformerContext == null)
        {
            return;
        }

        var movementComponents = FindObjectsOfType<CharacterMovement>(true);
        foreach (var movementComponent in movementComponents)
        {
            movementComponent.Initialize(platformerContext);
        }
    }

    private void ResolvePlayerReferences()
    {
        if (playerObject != null && playerHealth == null)
        {
            playerHealth = ResolveActorHealth(playerObject);
        }

        if (playerHealth != null)
        {
            if (playerObject == null)
            {
                playerObject = playerHealth.gameObject;
            }

            return;
        }

        var healthComponents = FindObjectsOfType<HealthComponent>(true);
        foreach (var healthComponent in healthComponents)
        {
            if (healthComponent != null && healthComponent.Faction == GameplayFaction.Player)
            {
                playerHealth = healthComponent;
                playerObject = healthComponent.gameObject;
                return;
            }
        }
    }

    private bool TryBuildTimelineGameplayEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;

        switch (clip.EventType)
        {
            case GameplayEventTimelineType.Damage:
                return TryBuildDamageEvent(clip, actorRoot, out gameplayEvent);

            case GameplayEventTimelineType.PlaySfx:
                return TryBuildPlaySfxEvent(clip, actorRoot, out gameplayEvent);

            case GameplayEventTimelineType.SpawnVfx:
                return TryBuildSpawnVfxEvent(clip, actorRoot, out gameplayEvent);

            case GameplayEventTimelineType.AttackTimelineFinished:
                return TryBuildAttackTimelineFinishedEvent(clip, actorRoot, out gameplayEvent);

            case GameplayEventTimelineType.EnemyDestroyed:
                return TryBuildEnemyDestroyedEvent(clip, actorRoot, out gameplayEvent);

            case GameplayEventTimelineType.EnemyRevive:
                return TryBuildEnemyReviveEvent(clip, actorRoot, out gameplayEvent);

            case GameplayEventTimelineType.EnemyRevived:
                return TryBuildEnemyRevivedEvent(clip, actorRoot, out gameplayEvent);

            default:
                Debug.LogWarning($"{nameof(GameStateManager)} does not support timeline event type {clip.EventType}.", this);
                return false;
        }
    }

    private bool TryBuildDamageEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        var target = ResolveHealthTarget(actorRoot, clip.TargetRole);
        if (target == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} cannot build Damage event without a self health target.", this);
            return false;
        }

        gameplayEvent = new DamageGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            target,
            clip.DamageAmount,
            clip.AllowFriendlyFire,
            target.gameObject,
            ResolveActorCombatSettings(actorRoot));
        return true;
    }

    private bool TryBuildPlaySfxEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        if (clip.AudioClip == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} cannot build PlaySfx event without an audio clip.", this);
            return false;
        }

        gameplayEvent = new PlaySfxGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            ResolveGameObjectTarget(actorRoot, clip.TargetRole),
            clip.AudioClip,
            clip.AudioVolume);
        return true;
    }

    private bool TryBuildSpawnVfxEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        if (clip.VfxPrefab == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} cannot build SpawnVfx event without a prefab.", this);
            return false;
        }

        gameplayEvent = new SpawnVfxGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            ResolveGameObjectTarget(actorRoot, clip.TargetRole),
            clip.VfxPrefab,
            ResolveSpawnPoint(actorRoot, clip.VfxSpawnPoint),
            clip.VfxOffset,
            clip.VfxParentMode);
        return true;
    }

    private bool TryBuildAttackTimelineFinishedEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        var target = ResolveGameObjectTarget(actorRoot, clip.TargetRole);
        var timelineAnimator = ResolveTimelineAnimator(actorRoot);
        if (target == null || timelineAnimator == null)
        {
            Debug.LogWarning(
                $"{nameof(GameStateManager)} cannot build AttackTimelineFinished event without a self target and {nameof(CharacterTimelineAnimator)}.",
                this);
            return false;
        }

        gameplayEvent = new AttackTimelineFinishedGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            target,
            timelineAnimator,
            clip.FinishedState);
        return true;
    }

    private bool TryBuildEnemyDestroyedEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        var target = ResolveGameObjectTarget(actorRoot, clip.TargetRole);
        var targetHealth = ResolveHealthTarget(actorRoot, clip.TargetRole);
        if (target == null || targetHealth == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} cannot build EnemyDestroyed event without a self target and health component.", this);
            return false;
        }

        gameplayEvent = new EnemyDestroyedGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            target,
            targetHealth);
        return true;
    }

    private bool TryBuildEnemyReviveEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        var target = ResolveGameObjectTarget(actorRoot, clip.TargetRole);
        var targetHealth = ResolveHealthTarget(actorRoot, clip.TargetRole);
        if (target == null || targetHealth == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} cannot build EnemyRevive event without a self target and health component.", this);
            return false;
        }

        gameplayEvent = new EnemyReviveGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            target,
            targetHealth);
        return true;
    }

    private bool TryBuildEnemyRevivedEvent(
        GameplayEventTimelineClip clip,
        GameObject actorRoot,
        out GameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        var target = ResolveGameObjectTarget(actorRoot, clip.TargetRole);
        var targetHealth = ResolveHealthTarget(actorRoot, clip.TargetRole);
        if (target == null || targetHealth == null)
        {
            Debug.LogWarning($"{nameof(GameStateManager)} cannot build EnemyRevived event without a self target and health component.", this);
            return false;
        }

        gameplayEvent = new EnemyRevivedGameplayEvent(
            CreateTimelineInstigator(actorRoot),
            target,
            targetHealth);
        return true;
    }

    private GameplayInstigator CreateTimelineInstigator(GameObject actorRoot)
    {
        var healthComponent = ResolveActorHealth(actorRoot);

        if (healthComponent != null)
        {
            return healthComponent.CreateInstigator();
        }

        return new GameplayInstigator(actorRoot, ResolveEntityMetadata(actorRoot));
    }

    private static HealthComponent ResolveHealthTarget(GameObject actorRoot, GameplayEventTargetRole role)
    {
        return role == GameplayEventTargetRole.Self ? ResolveActorHealth(actorRoot) : null;
    }

    private static GameObject ResolveGameObjectTarget(GameObject actorRoot, GameplayEventTargetRole role)
    {
        return role == GameplayEventTargetRole.Self ? actorRoot : null;
    }

    private static Transform ResolveSpawnPoint(GameObject actorRoot, Transform overrideSpawnPoint)
    {
        if (overrideSpawnPoint != null)
        {
            return overrideSpawnPoint;
        }

        return actorRoot != null ? actorRoot.transform : null;
    }

    private static HealthComponent ResolveActorHealth(GameObject actorRoot)
    {
        if (actorRoot == null)
        {
            return null;
        }

        return actorRoot.GetComponent<HealthComponent>()
            ?? actorRoot.GetComponentInChildren<HealthComponent>(true);
    }

    private static CharacterTimelineAnimator ResolveTimelineAnimator(GameObject actorRoot)
    {
        if (actorRoot == null)
        {
            return null;
        }

        return actorRoot.GetComponent<CharacterTimelineAnimator>()
            ?? actorRoot.GetComponentInChildren<CharacterTimelineAnimator>(true);
    }

    private static EntityMetadata ResolveEntityMetadata(GameObject actorRoot)
    {
        if (actorRoot == null)
        {
            return null;
        }

        var combatComponent = actorRoot.GetComponent<CombatComponent>()
            ?? actorRoot.GetComponentInChildren<CombatComponent>(true);

        return combatComponent != null ? combatComponent.EntityMetadata : null;
    }

    private static CombatSettings ResolveActorCombatSettings(GameObject actorRoot)
    {
        if (actorRoot == null)
        {
            return null;
        }

        var combatComponent = actorRoot.GetComponent<CombatComponent>()
            ?? actorRoot.GetComponentInChildren<CombatComponent>(true);

        return combatComponent != null ? combatComponent.CombatSettings : null;
    }
    
    public void RequestHitstop(float timeScale, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        StartCoroutine(RunHitstop(timeScale, duration));
    }

    private IEnumerator RunHitstop(float timeScale, float duration)
    {
        if (activeHitstopRequests == 0)
        {
            timeScaleBeforeHitstop = Time.timeScale;
        }

        activeHitstopRequests++;
        Time.timeScale = Mathf.Clamp01(timeScale);

        yield return new WaitForSecondsRealtime(duration);

        activeHitstopRequests = Mathf.Max(0, activeHitstopRequests - 1);

        if (activeHitstopRequests == 0)
        {
            Time.timeScale = timeScaleBeforeHitstop;
        }
    }
    
    private void ResetHitstop()
    {
        activeHitstopRequests = 0;
        timeScaleBeforeHitstop = 1f;

        if (!IsPlayerDead)
        {
            Time.timeScale = 1f;
        }
    }
    
    private void OnDestroy()
    {
        subscriptions.Dispose();

        if (Current == this)
        {
            Current = null;
        }
    }
}
