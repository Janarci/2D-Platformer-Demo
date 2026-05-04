using UnityEngine;

[CreateAssetMenu(menuName = "Platformer/Combat/Health Feedback Settings")]
public sealed class HealthFeedbackSettings : ScriptableObject
{
    [Header("Sprite Flash")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField, Min(0f)] private float flashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private Color deathFlashColor = Color.white;
    [SerializeField] private bool useSpriteColorFallback = true;

    public bool FlashOnDamage => flashOnDamage;
    public float FlashDuration => flashDuration;
    public Color HitFlashColor => hitFlashColor;
    public Color DeathFlashColor => deathFlashColor;
    public bool UseSpriteColorFallback => useSpriteColorFallback;
}
