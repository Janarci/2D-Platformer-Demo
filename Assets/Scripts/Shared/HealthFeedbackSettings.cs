using UnityEngine;

[CreateAssetMenu(menuName = "Platformer/Combat/Health Feedback Settings")]
public sealed class HealthFeedbackSettings : ScriptableObject
{
    [Header("Sprite Flash")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField, Min(0f)] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private bool useMaterialPropertyBlock = true;
    [SerializeField] private string flashAmountShaderProperty = "_FlashAmount";
    [SerializeField] private string flashColorShaderProperty = "_FlashColor";
    [SerializeField] private bool useSpriteColorFallback = true;

    public bool FlashOnDamage => flashOnDamage;
    public float FlashDuration => flashDuration;
    public Color FlashColor => flashColor;
    public bool UseMaterialPropertyBlock => useMaterialPropertyBlock;
    public string FlashAmountShaderProperty => flashAmountShaderProperty;
    public string FlashColorShaderProperty => flashColorShaderProperty;
    public bool UseSpriteColorFallback => useSpriteColorFallback;
}
