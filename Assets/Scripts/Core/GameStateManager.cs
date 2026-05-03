using UnityEngine;

public sealed class GameStateManager : MonoBehaviour
{
    private GameplayEventBus gameplayEventBus;

    public static GameStateManager Current { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning(
                $"Multiple {nameof(GameStateManager)} instances exist. {nameof(GameStateManager)}.{nameof(Current)} will keep using the first active instance.",
                this);
            return;
        }

        Current = this;
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus != null
            ? eventBus
            : throw new MissingReferenceException($"{nameof(GameStateManager)} requires a {nameof(GameplayEventBus)}.");

        IsInitialized = true;
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

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }
}
