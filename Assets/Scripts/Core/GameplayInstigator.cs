using UnityEngine;

public readonly struct GameplayInstigator
{
    public GameplayInstigator(
        GameObject owner,
        EntityMetadata metadata = null,
        GameplayFaction fallbackFaction = GameplayFaction.Neutral)
    {
        Owner = owner;
        Metadata = metadata;
        Faction = metadata != null ? metadata.Faction : fallbackFaction;
    }

    public GameplayInstigator(GameObject owner, GameplayFaction fallbackFaction)
        : this(owner, null, fallbackFaction)
    {
    }

    public static GameplayInstigator None => new GameplayInstigator(null);

    public GameObject Owner { get; }
    public EntityMetadata Metadata { get; }
    public GameplayFaction Faction { get; }
    public bool IsValid => Owner != null && Owner.activeInHierarchy;

    public bool IsSelf(GameObject target)
    {
        return Owner != null && target != null && Owner == target;
    }

    public bool IsSameFactionAs(GameplayInstigator other)
    {
        return Faction != GameplayFaction.Neutral
            && other.Faction != GameplayFaction.Neutral
            && Faction == other.Faction;
    }

    public bool CanAffect(GameplayInstigator target, bool allowSelf = false, bool allowFriendlyFire = false)
    {
        if (!IsValid || !target.IsValid)
        {
            return false;
        }

        if (!allowSelf && Owner == target.Owner)
        {
            return false;
        }

        if (!allowFriendlyFire && IsSameFactionAs(target))
        {
            return false;
        }

        return true;
    }
}

public enum GameplayFaction
{
    Neutral,
    Player,
    Enemy,
    Environment
}
