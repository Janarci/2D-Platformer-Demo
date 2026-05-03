using System.Collections;
using UniRx;
using UnityEngine;

public sealed class EnemyLifecycleController : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private CharacterTimelineAnimator timelineAnimator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField, Min(0f)] private float reviveDelay = 5f;
    [SerializeField] private bool resetToSpawnPoint = true;

    private readonly CompositeDisposable subscriptions = new();
    private Collider2D[] collidersToToggle;
    private Renderer[] renderersToToggle;
    private bool[] initialColliderEnabledStates;
    private bool[] initialRendererEnabledStates;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool initialEnemyControllerEnabled;
    private bool initialEnemyMovementEnabled;
    private bool initialStateMachineEnabled;
    private bool initialTimelineAnimatorEnabled;
    private bool initialBodySimulated;
    private bool isDead;
    private bool isObservingGameplayEvents;

    private void Awake()
    {
        ResolveReferences();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        collidersToToggle = GetComponentsInChildren<Collider2D>(true);
        renderersToToggle = GetComponentsInChildren<Renderer>(true);
        initialColliderEnabledStates = CaptureEnabledStates(collidersToToggle);
        initialRendererEnabledStates = CaptureEnabledStates(renderersToToggle);
        initialEnemyControllerEnabled = enemyController != null && enemyController.enabled;
        initialEnemyMovementEnabled = enemyMovement != null && enemyMovement.enabled;
        initialStateMachineEnabled = stateMachine != null && stateMachine.enabled;
        initialTimelineAnimatorEnabled = timelineAnimator != null && timelineAnimator.enabled;
        initialBodySimulated = body == null || body.simulated;

        if (healthComponent != null)
        {
            healthComponent.Died += HandleDied;
        }

        BindGameplayEvents();
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus;
        BindGameplayEvents();
    }

    private void ResolveReferences()
    {
        if (!healthComponent)
        {
            healthComponent = GetComponent<HealthComponent>();
        }

        if (!enemyController)
        {
            enemyController = GetComponent<EnemyController>();
        }

        if (!enemyMovement)
        {
            enemyMovement = GetComponent<EnemyMovement>();
        }

        if (!stateMachine)
        {
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        if (!timelineAnimator)
        {
            timelineAnimator = GetComponent<CharacterTimelineAnimator>();
        }

        if (!body)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void BindGameplayEvents()
    {
        if (isObservingGameplayEvents || gameplayEventBus == null)
        {
            return;
        }

        isObservingGameplayEvents = true;

        gameplayEventBus
            .Observe<EnemyReviveGameplayEvent>()
            .Where(IsOwnReviveEvent)
            .Subscribe(_ => Revive())
            .AddTo(subscriptions);
    }

    private bool IsOwnReviveEvent(EnemyReviveGameplayEvent gameplayEvent)
    {
        return gameplayEvent.Enemy == gameObject
            || gameplayEvent.HealthComponent == healthComponent;
    }

    private void HandleDied(HealthComponent deadHealth, GameplayInstigator instigator)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        PublishDestroyedEvent();
        SetDeadPresentation(true);
        ScheduleRevive();
    }

    private void PublishDestroyedEvent()
    {
        if (!CanUseGameplayEventBus())
        {
            return;
        }

        gameplayEventBus.FireInstant(
            new EnemyDestroyedGameplayEvent(CreateSelfInstigator(), gameObject, healthComponent));
    }

    private void ScheduleRevive()
    {
        if (CanUseGameplayEventBus())
        {
            gameplayEventBus.Register(
                new EnemyReviveGameplayEvent(CreateSelfInstigator(), gameObject, healthComponent),
                reviveDelay);
            return;
        }

        StartCoroutine(ReviveAfterDelay());
    }

    private IEnumerator ReviveAfterDelay()
    {
        yield return new WaitForSeconds(reviveDelay);
        Revive();
    }

    private void Revive()
    {
        if (!isDead)
        {
            return;
        }

        if (resetToSpawnPoint)
        {
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        }

        SetDeadPresentation(false);
        healthComponent?.ReviveFull();
        stateMachine?.ResetState();
        enemyController?.ResetBehavior();
        isDead = false;
        PublishRevivedEvent();
    }

    private void SetDeadPresentation(bool dead)
    {
        if (stateMachine != null)
        {
            stateMachine.HandleAttackInterrupted();
            stateMachine.enabled = !dead && initialStateMachineEnabled;
        }

        if (timelineAnimator != null)
        {
            if (dead)
            {
                timelineAnimator.StopCurrentTimeline();
            }

            timelineAnimator.enabled = !dead && initialTimelineAnimatorEnabled;
        }

        if (enemyController != null)
        {
            enemyController.enabled = !dead && initialEnemyControllerEnabled;
        }

        if (enemyMovement != null)
        {
            enemyMovement.StopHorizontalMove();
            enemyMovement.enabled = !dead && initialEnemyMovementEnabled;
        }

        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.simulated = !dead && initialBodySimulated;
        }

        for (var i = 0; i < collidersToToggle.Length; i++)
        {
            var enemyCollider = collidersToToggle[i];
            if (enemyCollider != null)
            {
                enemyCollider.enabled = !dead && initialColliderEnabledStates[i];
            }
        }

        for (var i = 0; i < renderersToToggle.Length; i++)
        {
            var enemyRenderer = renderersToToggle[i];
            if (enemyRenderer != null)
            {
                enemyRenderer.enabled = !dead && initialRendererEnabledStates[i];
            }
        }
    }

    private void PublishRevivedEvent()
    {
        if (!CanUseGameplayEventBus())
        {
            return;
        }

        gameplayEventBus.FireInstant(
            new EnemyRevivedGameplayEvent(CreateSelfInstigator(), gameObject, healthComponent));
    }

    private bool CanUseGameplayEventBus()
    {
        return gameplayEventBus != null && gameplayEventBus.IsInitialized;
    }

    private GameplayInstigator CreateSelfInstigator()
    {
        return healthComponent != null
            ? healthComponent.CreateInstigator()
            : new GameplayInstigator(gameObject, GameplayFaction.Enemy);
    }

    private static bool[] CaptureEnabledStates(Collider2D[] colliders)
    {
        var states = new bool[colliders.Length];

        for (var i = 0; i < colliders.Length; i++)
        {
            states[i] = colliders[i] != null && colliders[i].enabled;
        }

        return states;
    }

    private static bool[] CaptureEnabledStates(Renderer[] renderers)
    {
        var states = new bool[renderers.Length];

        for (var i = 0; i < renderers.Length; i++)
        {
            states[i] = renderers[i] != null && renderers[i].enabled;
        }

        return states;
    }

    private void OnDestroy()
    {
        if (healthComponent != null)
        {
            healthComponent.Died -= HandleDied;
        }

        subscriptions.Dispose();
    }
}
