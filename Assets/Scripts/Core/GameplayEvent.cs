using UnityEngine;

public abstract class GameplayEvent
{
    protected GameplayEvent(
        GameplayInstigator instigator,
        GameObject targetObject = null,
        string eventId = null)
    {
        Instigator = instigator;
        TargetObject = targetObject;
        EventId = string.IsNullOrWhiteSpace(eventId)
            ? GetType().Name
            : eventId;
    }

    public GameplayInstigator Instigator { get; }
    public GameObject TargetObject { get; }
    public string EventId { get; }
    public float ExecuteAtTime { get; private set; }
    public bool HasTarget => TargetObject != null;

    public virtual bool CanExecute(GameplayEventContext context)
    {
        return context != null
            && Instigator.IsValid
            && (!HasTarget || TargetObject.activeInHierarchy);
    }

    public void Execute(GameplayEventContext context)
    {
        var ownerName = Instigator.Owner != null
            ? Instigator.Owner.name
            : "Unknown";
        
        Debug.LogError(
            $"{nameof(GameplayEvent)}.{nameof(Execute)} was called by {ownerName}. " +
            "Gameplay events should be handled through UniRx observers instead.");
    }

    internal void SetExecuteAtTime(float executeAtTime)
    {
        ExecuteAtTime = Mathf.Max(0f, executeAtTime);
    }
}

public sealed class PlayerDiedEvent : GameplayEvent
{
    public PlayerDiedEvent(GameplayInstigator instigator,
        GameObject player)
        : base(instigator, player)
    {
        Player = player;
    }
    
    public GameObject Player { get;}
}

public sealed class PlayerHitEvent : GameplayEvent
{
    public PlayerHitEvent(GameplayInstigator instigator, 
        GameObject player, 
        int damage)
        : base(instigator, player)
    {
        Player = player;
        Damage = damage;
    }
    public GameObject Player { get;}
    public int Damage { get;}
}

public sealed class AttackTimelineFinishedGameplayEvent : GameplayEvent
{
    public AttackTimelineFinishedGameplayEvent(
        GameplayInstigator instigator,
        GameObject character,
        CharacterTimelineAnimator timelineAnimator,
        CharacterState finishedState)
        : base(instigator, character)
    {
        Character = character;
        TimelineAnimator = timelineAnimator;
        FinishedState = finishedState;
    }

    public GameObject Character { get; }
    public CharacterTimelineAnimator TimelineAnimator { get; }
    public CharacterState FinishedState { get; }

    public override bool CanExecute(GameplayEventContext context)
    {
        return base.CanExecute(context)
            && Character != null
            && TimelineAnimator != null
            && IsAttackState(FinishedState);
    }
    
    private static bool IsAttackState(CharacterState state)
    {
        return state == CharacterState.Attack
            || state == CharacterState.AttackUp
            || state == CharacterState.JumpAttack
            || state == CharacterState.JumpAttackUp
            || state == CharacterState.JumpAttackDown;
    }
}

public sealed class CharacterStateTimelineFinishedGameplayEvent : GameplayEvent
{
    public CharacterStateTimelineFinishedGameplayEvent(
        GameplayInstigator instigator,
        GameObject character,
        CharacterTimelineAnimator timelineAnimator,
        CharacterState finishedState)
        : base(instigator, character)
    {
        Character = character;
        TimelineAnimator = timelineAnimator;
        FinishedState = finishedState;
    }

    public GameObject Character { get; }
    public CharacterTimelineAnimator TimelineAnimator { get; }
    public CharacterState FinishedState { get; }

    public override bool CanExecute(GameplayEventContext context)
    {
        return base.CanExecute(context)
            && Character != null
            && TimelineAnimator != null;
    }
}

public sealed class PlaySfxGameplayEvent : GameplayEvent
{
    public PlaySfxGameplayEvent(
        GameplayInstigator instigator,
        GameObject targetObject,
        AudioClip clip,
        float volume = 1f)
        : base(instigator, targetObject)
    {
        Clip = clip;
        Volume = volume;
    }

    public AudioClip Clip { get; }
    public float Volume { get; }

    public override bool CanExecute(GameplayEventContext context)
    {
        return base.CanExecute(context)
            && Clip != null;
    }
}

public sealed class SpawnVfxGameplayEvent : GameplayEvent
{
    public SpawnVfxGameplayEvent(
        GameplayInstigator instigator,
        GameObject targetObject,
        GameObject prefab,
        Transform spawnPoint,
        Vector3 offset,
        GameplayEventVfxParentMode parentMode)
        : base(instigator, targetObject)
    {
        Prefab = prefab;
        SpawnPoint = spawnPoint;
        Offset = offset;
        ParentMode = parentMode;
    }

    public GameObject Prefab { get; }
    public Transform SpawnPoint { get; }
    public Vector3 Offset { get; }
    public GameplayEventVfxParentMode ParentMode { get; }

    public override bool CanExecute(GameplayEventContext context)
    {
        return base.CanExecute(context)
            && Prefab != null;
    }
}

public sealed class EnemyDestroyedGameplayEvent : GameplayEvent
{
    public EnemyDestroyedGameplayEvent(
        GameplayInstigator instigator,
        GameObject enemy,
        HealthComponent healthComponent)
        : base(instigator, enemy)
    {
        Enemy = enemy;
        HealthComponent = healthComponent;
    }

    public GameObject Enemy { get; }
    public HealthComponent HealthComponent { get; }
}

public sealed class EnemyReviveGameplayEvent : GameplayEvent
{
    public EnemyReviveGameplayEvent(
        GameplayInstigator instigator,
        GameObject enemy,
        HealthComponent healthComponent)
        : base(instigator, enemy)
    {
        Enemy = enemy;
        HealthComponent = healthComponent;
    }

    public GameObject Enemy { get; }
    public HealthComponent HealthComponent { get; }
}

public sealed class EnemyRevivedGameplayEvent : GameplayEvent
{
    public EnemyRevivedGameplayEvent(
        GameplayInstigator instigator,
        GameObject enemy,
        HealthComponent healthComponent)
        : base(instigator, enemy)
    {
        Enemy = enemy;
        HealthComponent = healthComponent;
    }

    public GameObject Enemy { get; }
    public HealthComponent HealthComponent { get; }
}

public sealed class DamageGameplayEvent : GameplayEvent
{
    public DamageGameplayEvent(
        GameplayInstigator instigator,
        HealthComponent target,
        int amount,
        bool allowFriendlyFire = false,
        GameObject targetObject = null,
        CombatSettings combatSettings = null)
        : base(instigator, targetObject != null ? targetObject : target != null ? target.gameObject : null)
    {
        Target = target;
        Amount = amount;
        AllowFriendlyFire = allowFriendlyFire;
        CombatSettings = combatSettings;
    }

    public HealthComponent Target { get; }
    public int Amount { get; }
    public bool AllowFriendlyFire { get; }
    public CombatSettings CombatSettings { get; }

    public override bool CanExecute(GameplayEventContext context)
    {
        return base.CanExecute(context)
            && Target != null
            && Target.IsAlive;
    }

}
