using System;
using System.Collections;
using UnityEngine;

public sealed class HealthComponent : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField] private GameplayFaction faction = GameplayFaction.Neutral;
    [SerializeField] private bool allowSelfDamage;
    [SerializeField] private bool allowFriendlyFire;
    [SerializeField] private HealthFeedbackSettings feedbackSettings;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private GameplayEventBus gameplayEventBus;

    private Color[] originalSpriteColors;
    private Coroutine flashRoutine;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public HealthFeedbackSettings FeedbackSettings => feedbackSettings;
    public GameplayFaction Faction
    {
        get
        {
            var metadata = ResolveEntityMetadata();
            return metadata != null ? metadata.Faction : faction;
        }
    }
    public bool IsAlive => CurrentHealth > 0;

    private void Awake()
    {
        ResolveReferences();
        CaptureOriginalSpriteColors();
        ResetHealth();
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus;
    }

    public void ResetHealth()
    {
        var previousHealth = CurrentHealth;
        CurrentHealth = maxHealth;
        PublishHealthUpdated(previousHealth, CreateInstigator());
    }

    public void ReviveFull()
    {
        var wasAlive = IsAlive;
        var previousHealth = CurrentHealth;
        CurrentHealth = maxHealth;
        var instigator = CreateInstigator();
        PublishHealthUpdated(previousHealth, instigator);

        if (!wasAlive)
        {
            PublishHealthRevived(instigator);
        }
    }

    public GameplayInstigator CreateInstigator()
    {
        return new GameplayInstigator(gameObject, ResolveEntityMetadata(), faction);
    }

    public bool ApplyDamage(int amount, GameplayInstigator instigator)
    {
        return ApplyDamage(amount, instigator, allowFriendlyFire);
    }

    public bool ApplyDamage(int amount, GameplayInstigator instigator, bool allowFriendlyFireFromPayload)
    {
        if (amount <= 0 || !IsAlive)
        {
            return false;
        }

        var target = CreateInstigator();
        if (!instigator.CanAffect(target, allowSelfDamage, allowFriendlyFire || allowFriendlyFireFromPayload))
        {
            return false;
        }

        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        PublishHealthDamaged(amount, previousHealth, instigator);
        PublishHealthUpdated(previousHealth, instigator);
        ApplySpriteFlash();

        if (CurrentHealth == 0)
        {
            PublishHealthDied(instigator);
        }

        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return;
        }

        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        PublishHealthUpdated(previousHealth, CreateInstigator());
    }

    private EntityMetadata ResolveEntityMetadata()
    {
        var combatComponent = GetComponent<CombatComponent>()
            ?? GetComponentInChildren<CombatComponent>(true)
            ?? GetComponentInParent<CombatComponent>();

        return combatComponent != null ? combatComponent.EntityMetadata : null;
    }

    private void ResolveReferences()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    private void ApplySpriteFlash()
    {
        if (feedbackSettings == null || !feedbackSettings.FlashOnDamage || feedbackSettings.FlashDuration <= 0f)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            RestoreSpriteFlash();
        }
        CaptureOriginalSpriteColors();
        flashRoutine = StartCoroutine(RunSpriteFlash());
    }

    private IEnumerator RunSpriteFlash()
    {
        SetSpriteFlash(1f);

        yield return new WaitForSecondsRealtime(feedbackSettings.FlashDuration);

        RestoreSpriteFlash();
        flashRoutine = null;
    }

    private void SetSpriteFlash(float amount)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        foreach (var spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null || spriteRenderer.enabled == false)
            {
                continue;
            }

            if (feedbackSettings.UseSpriteColorFallback)
            {
                spriteRenderer.color = feedbackSettings.HitFlashColor;
            }
        }
    }

    private void RestoreSpriteFlash()
    {
        if (spriteRenderers == null || originalSpriteColors == null)
        {
            return;
        }

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            var spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null || i >= originalSpriteColors.Length)
            {
                continue;
            }

            spriteRenderer.color = originalSpriteColors[i];
        }
    }

    private void CaptureOriginalSpriteColors()
    {
        if (spriteRenderers == null)
        {
            originalSpriteColors = Array.Empty<Color>();
            return;
        }

        originalSpriteColors = new Color[spriteRenderers.Length];

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i] != null
                ? spriteRenderers[i].color
                : Color.white;
        }
    }

    private void PublishHealthDamaged(
        int damage,
        int previousHealth,
        GameplayInstigator instigator)
    {
        if (!CanPublishGameplayEvent())
        {
            return;
        }

        gameplayEventBus.FireInstant(
            new HealthDamagedGameplayEvent(
                instigator,
                this,
                damage,
                previousHealth,
                CurrentHealth,
                maxHealth));
    }

    private void PublishHealthUpdated(int previousHealth, GameplayInstigator instigator)
    {
        if (previousHealth == CurrentHealth || !CanPublishGameplayEvent())
        {
            return;
        }

        gameplayEventBus.FireInstant(
            new HealthUpdatedGameplayEvent(
                instigator,
                this,
                previousHealth,
                CurrentHealth,
                maxHealth));
    }

    private void PublishHealthDied(GameplayInstigator instigator)
    {
        if (!CanPublishGameplayEvent())
        {
            return;
        }

        gameplayEventBus.FireInstant(new HealthDiedGameplayEvent(instigator, this));
    }

    private void PublishHealthRevived(GameplayInstigator instigator)
    {
        if (!CanPublishGameplayEvent())
        {
            return;
        }

        gameplayEventBus.FireInstant(new HealthRevivedGameplayEvent(instigator, this));
    }

    private bool CanPublishGameplayEvent()
    {
        return gameplayEventBus != null && gameplayEventBus.IsInitialized;
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreSpriteFlash();
        
    }
    private void OnDestroy()
    {
        RestoreSpriteFlash();
    }
}
