using UnityEngine;

[CreateAssetMenu(menuName = "Platformer/Combat/Combat Settings")]
public sealed class CombatSettings : ScriptableObject
{
    [Header("Damage")]
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float damageDelay;
    [SerializeField] private bool allowFriendlyFire;

    [Header("Knockback")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField, Min(0f)] private float horizontalKnockbackVelocity = 6f;
    [SerializeField, Min(0f)] private float verticalHopVelocity = 3f;
    [SerializeField] private bool preserveHigherVerticalVelocity = true;

    [Header("Hitstop")]
    [SerializeField] private bool applyHitstop = true;
    [SerializeField, Range(0f, 1f)] private float hitstopTimeScale = 0.05f;
    [SerializeField, Min(0f)] private float hitstopDuration = 0.06f;

    public int Damage => damage;
    public float DamageDelay => damageDelay;
    public bool AllowFriendlyFire => allowFriendlyFire;
    public bool ApplyKnockback => applyKnockback;
    public float HorizontalKnockbackVelocity => horizontalKnockbackVelocity;
    public float VerticalHopVelocity => verticalHopVelocity;
    public bool PreserveHigherVerticalVelocity => preserveHigherVerticalVelocity;
    public bool ApplyHitstop => applyHitstop;
    public float HitstopTimeScale => hitstopTimeScale;
    public float HitstopDuration => hitstopDuration;
}
