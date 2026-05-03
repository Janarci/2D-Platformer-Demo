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

    public event Action<HealthComponent, int, GameplayInstigator> Damaged;
    public event Action<HealthComponent, GameplayInstigator> Died;
    public event Action<HealthComponent> Revived;

    private MaterialPropertyBlock flashPropertyBlock;
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

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
    }

    public void ReviveFull()
    {
        var wasAlive = IsAlive;
        CurrentHealth = maxHealth;

        if (!wasAlive)
        {
            Revived?.Invoke(this);
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

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Damaged?.Invoke(this, amount, instigator);
        ApplySpriteFlash();

        if (CurrentHealth == 0)
        {
            Died?.Invoke(this, instigator);
        }

        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
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
            RestoreSpriteFlash();
        }

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

        flashPropertyBlock ??= new MaterialPropertyBlock();

        foreach (var spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            if (feedbackSettings.UseMaterialPropertyBlock)
            {
                spriteRenderer.GetPropertyBlock(flashPropertyBlock);
                flashPropertyBlock.SetFloat(feedbackSettings.FlashAmountShaderProperty, amount);
                flashPropertyBlock.SetColor(feedbackSettings.FlashColorShaderProperty, feedbackSettings.FlashColor);
                spriteRenderer.SetPropertyBlock(flashPropertyBlock);
            }

            if (feedbackSettings.UseSpriteColorFallback)
            {
                spriteRenderer.color = feedbackSettings.FlashColor;
            }
        }
    }

    private void RestoreSpriteFlash()
    {
        if (spriteRenderers == null)
        {
            return;
        }

        flashPropertyBlock ??= new MaterialPropertyBlock();

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            var spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            if (feedbackSettings != null && feedbackSettings.UseMaterialPropertyBlock)
            {
                spriteRenderer.GetPropertyBlock(flashPropertyBlock);
                flashPropertyBlock.SetFloat(feedbackSettings.FlashAmountShaderProperty, 0f);
                spriteRenderer.SetPropertyBlock(flashPropertyBlock);
            }

            if (originalSpriteColors != null
                && i < originalSpriteColors.Length
                && feedbackSettings != null
                && feedbackSettings.UseSpriteColorFallback)
            {
                spriteRenderer.color = originalSpriteColors[i];
            }
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

    private void OnDestroy()
    {
        RestoreSpriteFlash();
    }
}
