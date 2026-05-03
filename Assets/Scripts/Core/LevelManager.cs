using UnityEngine;

public sealed class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelSettings levelSettings;

    private PlatformerContext platformerContext;

    public bool IsInitialized { get; private set; }
    public LevelSettings Settings => levelSettings;
    public PlatformerContext PlatformerContext => platformerContext;
    public Vector3 PlayerSpawnPosition => levelSettings != null ? levelSettings.PlayerSpawnPosition : transform.position;
    public float RespawnDelay => levelSettings != null ? levelSettings.RespawnDelay : 0f;
    public Bounds LevelBounds => levelSettings != null ? levelSettings.LevelBounds : new Bounds(transform.position, Vector3.zero);
    public bool AllowPlayerRespawn => levelSettings == null || levelSettings.AllowPlayerRespawn;

    public void Initialize(PlatformerContext context)
    {
        if (context == null)
        {
            throw new MissingReferenceException($"{nameof(LevelManager)} requires a {nameof(PlatformerContext)}.");
        }

        if (levelSettings == null)
        {
            throw new MissingReferenceException($"{nameof(LevelManager)} requires a {nameof(LevelSettings)} asset.");
        }

        platformerContext = context;
        IsInitialized = true;
    }

    public void SetLevelSettings(LevelSettings settings)
    {
        levelSettings = settings;
    }
}
