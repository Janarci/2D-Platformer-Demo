using UnityEngine;

[CreateAssetMenu(fileName = "LevelSettings", menuName = "Platformer/Level Settings")]
public sealed class LevelSettings : ScriptableObject
{
    [SerializeField] private Vector3 playerSpawnPosition;
    [SerializeField, Min(0f)] private float respawnDelay = 1.5f;
    [SerializeField] private Bounds levelBounds = new Bounds(Vector3.zero, new Vector3(100f, 50f, 0f));
    [SerializeField] private bool allowPlayerRespawn = true;

    public Vector3 PlayerSpawnPosition => playerSpawnPosition;
    public float RespawnDelay => respawnDelay;
    public Bounds LevelBounds => levelBounds;
    public bool AllowPlayerRespawn => allowPlayerRespawn;
}
