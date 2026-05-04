using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatComponent : MonoBehaviour
{
    private static int activeHitstopRequests;
    private static float timeScaleBeforeHitstop = 1f;

    [Header("References")]
    [SerializeField] private EntityMetadata entityMetadata;
    [SerializeField] private HealthComponent ownerHealth;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField] private Collider2D attackHitboxCollider;
    [SerializeField] private Collider2D hurtboxCollider;

    [Header("Settings")]
    [SerializeField] private CombatSettings combatSettings;
    [SerializeField, Min(0)] private int fallbackDamage = 1;
    [SerializeField, Min(0f)] private float fallbackDamageDelay;
    [SerializeField] private bool fallbackAllowFriendlyFire;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool hitEachTargetOncePerActivation = true;
    [SerializeField, Min(1)] private int overlapBufferSize = 16;

    private readonly HashSet<HealthComponent> hitTargets = new();
    private Collider2D[] overlapBuffer;
    private ContactFilter2D contactFilter;
    private bool wasAttackHitboxActive;

    public int Damage => combatSettings != null ? combatSettings.Damage : fallbackDamage;
    public CombatSettings CombatSettings => combatSettings;
    public EntityMetadata EntityMetadata => entityMetadata;
    public HealthComponent OwnerHealth => ownerHealth;
    public Collider2D HurtboxCollider => hurtboxCollider;
    
    private void Awake()
    {
        ResolveReferences();
        ConfigureOverlapQuery();
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus;
    }

    private void Update()
    {
        TickAttackHitbox();
    }

    public void SetAttackHitboxActive(bool active)
    {
        if (attackHitboxCollider == null)
        {
            return;
        }

        attackHitboxCollider.gameObject.gameObject.SetActive(active);

        if (active)
        {
            hitTargets.Clear();
        }

        wasAttackHitboxActive = active;
    }

    public bool TryRegisterDamage(CombatComponent targetCombat, float delaySeconds = -1f)
    {
        if (targetCombat == null
            || targetCombat == this
            || targetCombat.ownerHealth == null
            || !targetCombat.ownerHealth.IsAlive
            || gameplayEventBus == null
            || !gameplayEventBus.IsInitialized
            || Damage <= 0)
        {
            return false;
        }

        var instigator = CreateInstigator();
        var targetInstigator = targetCombat.CreateInstigator();
        if (!instigator.CanAffect(targetInstigator, false, AllowFriendlyFire))
        {
            return false;
        }

        if (hitEachTargetOncePerActivation && !hitTargets.Add(targetCombat.ownerHealth))
        {
            return false;
        }

        var gameplayEvent = new DamageGameplayEvent(
            instigator,
            targetCombat.ownerHealth,
            Damage,
            AllowFriendlyFire,
            targetCombat.gameObject,
            combatSettings);

        var delay = delaySeconds >= 0f ? delaySeconds : DamageDelay;
        gameplayEventBus.Register(gameplayEvent, delay);
        return true;
    }

    public GameplayInstigator CreateInstigator()
    {
        if (ownerHealth != null)
        {
            return ownerHealth.CreateInstigator();
        }

        return new GameplayInstigator(gameObject, entityMetadata);
    }

    public bool OwnsHurtbox(Collider2D candidate)
    {
        return hurtboxCollider != null && candidate == hurtboxCollider;
    }

    public void ApplyDamageFeedback(DamageGameplayEvent gameplayEvent)
    {
        ApplyKnockback(gameplayEvent);
        ApplyHitstop(gameplayEvent);
    }

    private float DamageDelay => combatSettings != null
        ? combatSettings.DamageDelay
        : fallbackDamageDelay;

    private bool AllowFriendlyFire => combatSettings != null
        ? combatSettings.AllowFriendlyFire
        : fallbackAllowFriendlyFire;

    private void ResolveReferences()
    {
        if (!ownerHealth)
        {
            ownerHealth = GetComponentInParent<HealthComponent>();
        }

        if (!body)
        {
            body = GetComponentInParent<Rigidbody2D>();
        }
        if (!gameplayEventBus)
        {
            gameplayEventBus = FindObjectOfType<GameplayEventBus>(true);
        }
    }

    private void ConfigureOverlapQuery()
    {
        overlapBuffer = new Collider2D[Mathf.Max(1, overlapBufferSize)];
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(targetLayers);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = true;
    }

    private void TickAttackHitbox()
    {
        var isAttackHitboxActive = attackHitboxCollider != null
            && attackHitboxCollider.enabled
            && attackHitboxCollider.gameObject.activeInHierarchy;

        if (isAttackHitboxActive && !wasAttackHitboxActive)
        {
            hitTargets.Clear();
        }

        wasAttackHitboxActive = isAttackHitboxActive;

        if (!isAttackHitboxActive)
        {
            return;
        }

        var overlapCount = attackHitboxCollider.OverlapCollider(contactFilter, overlapBuffer);
        for (var i = 0; i < overlapCount; i++)
        {
            TryRegisterHit(overlapBuffer[i]);
            overlapBuffer[i] = null;
        }
    }

    private void TryRegisterHit(Collider2D candidate)
    {
        if (candidate == null || candidate == attackHitboxCollider || candidate == hurtboxCollider)
        {
            return;
        }

        if (!candidate.isTrigger)
        {
            return;
        }
        
        var targetCombat = candidate.GetComponentInParent<CombatComponent>();
        if (targetCombat == null || !targetCombat.OwnsHurtbox(candidate))
        {
            return;
        }

        TryRegisterDamage(targetCombat, 0f);
    }

    private void ApplyKnockback(DamageGameplayEvent gameplayEvent)
    {
        var attackSettings = gameplayEvent.CombatSettings;
        if (attackSettings == null || !attackSettings.ApplyKnockback || body == null)
        {
            return;
        }

        var direction = GetKnockbackDirection(gameplayEvent);
        var horizontalVelocity = direction * attackSettings.HorizontalKnockbackVelocity;
        var verticalVelocity = attackSettings.PreserveHigherVerticalVelocity
            ? Mathf.Max(body.velocity.y, attackSettings.VerticalHopVelocity)
            : body.velocity.y + attackSettings.VerticalHopVelocity;

        body.velocity = new Vector2(body.velocity.x + horizontalVelocity, verticalVelocity);
    }

    private float GetKnockbackDirection(DamageGameplayEvent gameplayEvent)
    {
        var attacker = gameplayEvent.Instigator.Owner;
        if (attacker == null)
        {
            return transform.localScale.x < 0f ? 1f : -1f;
        }

        var direction = transform.position.x - attacker.transform.position.x;
        return Mathf.Abs(direction) > 0.01f ? Mathf.Sign(direction) : 1f;
    }

    private static IEnumerator RunHitstop(float timeScale, float duration)
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

    private void ApplyHitstop(DamageGameplayEvent gameplayEvent)
    {
        var attackSettings = gameplayEvent.CombatSettings;
        if (attackSettings == null || !attackSettings.ApplyHitstop || attackSettings.HitstopDuration <= 0f)
        {
            return;
        }

        StartCoroutine(RunHitstop(attackSettings.HitstopTimeScale, attackSettings.HitstopDuration));
    }
}
