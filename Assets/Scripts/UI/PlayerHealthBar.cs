using UniRx;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private HealthComponent playerHealth;
    [SerializeField] private RectTransform actualHealthbar;

    private readonly CompositeDisposable subscriptions = new();
    private bool isObserving;
    private float fullHealthbarWidth;

    private void Awake()
    {
        ResolveLocalReferences();
    }

    private void Start()
    {
        ResolveReferences();
        CaptureFullHealthbarWidth();
        RenderInitialState();
        BindGameplayEvents();
    }

    public void Initialize(GameplayEventBus eventBus, HealthComponent healthComponent)
    {
        gameplayEventBus = eventBus;
        playerHealth = healthComponent;
        playerObject = healthComponent != null ? healthComponent.gameObject : playerObject;
        ResolveLocalReferences();
        CaptureFullHealthbarWidth();
        RenderInitialState();
        BindGameplayEvents();
    }

    private void ResolveLocalReferences()
    {
        if (actualHealthbar != null)
        {
            return;
        }

        var childImages = GetComponentsInChildren<Image>(true);
        foreach (var childImage in childImages)
        {
            if (childImage != null && childImage.gameObject != gameObject)
            {
                actualHealthbar = childImage.rectTransform;
                return;
            }
        }
    }

    private void ResolveReferences()
    {
        var gameStateManager = GameStateManager.Current;
        if (gameStateManager == null)
        {
            return;
        }

        if (gameplayEventBus == null)
        {
            gameplayEventBus = gameStateManager.EventBus;
        }

        if (playerHealth == null)
        {
            playerHealth = gameStateManager.PlayerHealth;
        }

        if (playerHealth != null)
        {
            playerObject = playerHealth.gameObject;
        }
        else if (playerObject == null)
        {
            playerObject = gameStateManager.PlayerObject;
        }
    }

    private void BindGameplayEvents()
    {
        if (playerHealth != null)
        {
            playerObject = playerHealth.gameObject;
        }

        if (isObserving
            || gameplayEventBus == null
            || !gameplayEventBus.IsInitialized
            || playerObject == null)
        {
            return;
        }

        isObserving = true;

        gameplayEventBus
            .ObserveTarget<HealthUpdatedGameplayEvent>(playerObject)
            .Subscribe(HandleHealthUpdated)
            .AddTo(subscriptions);

        gameplayEventBus
            .ObserveTarget<HealthDiedGameplayEvent>(playerObject)
            .Subscribe(HandleHealthDied)
            .AddTo(subscriptions);
    }

    private void HandleHealthUpdated(HealthUpdatedGameplayEvent gameplayEvent)
    {
        if (playerHealth == null)
        {
            playerHealth = gameplayEvent.HealthComponent;
        }

        if (playerObject == null)
        {
            playerObject = gameplayEvent.TargetObject;
        }

        RenderHealth(
            gameplayEvent.CurrentHealth,
            gameplayEvent.MaxHealth,
            gameplayEvent.NormalizedHealth);
    }

    private void HandleHealthDied(HealthDiedGameplayEvent gameplayEvent)
    {
        if (playerHealth == null)
        {
            playerHealth = gameplayEvent.HealthComponent;
        }

        if (playerObject == null)
        {
            playerObject = gameplayEvent.TargetObject;
        }

        var maxHealth = playerHealth != null ? playerHealth.MaxHealth : 1;
        RenderHealth(0, maxHealth, 0f);
    }

    private void RenderInitialState()
    {
        if (playerHealth == null)
        {
            return;
        }

        var normalizedHealth = playerHealth.MaxHealth > 0
            ? Mathf.Clamp01((float)playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;

        RenderHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth, normalizedHealth);
    }

    private void RenderHealth(int currentHealth, int maxHealth, float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);

        if (actualHealthbar != null)
        {
            var sizeDelta = actualHealthbar.sizeDelta;
            sizeDelta.x = fullHealthbarWidth * normalizedHealth;
            actualHealthbar.sizeDelta = sizeDelta;
        }
    }

    private void CaptureFullHealthbarWidth()
    {
        if (actualHealthbar == null)
        {
            return;
        }

        fullHealthbarWidth = actualHealthbar.sizeDelta.x;

        if (fullHealthbarWidth <= 0f)
        {
            fullHealthbarWidth = actualHealthbar.rect.width;
        }
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
    }
}
