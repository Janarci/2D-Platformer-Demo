using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public sealed class GameplayEventTimelineClip : PlayableAsset, ITimelineClipAsset
{
    [SerializeField] private GameplayEventTimelineType eventType;
    [SerializeField] private GameplayEventTargetRole targetRole = GameplayEventTargetRole.Self;
    [SerializeField] private bool fireInEditorPreview;

    [SerializeField, Min(0)] private int damageAmount = 1;
    [SerializeField] private bool allowFriendlyFire;

    [SerializeField] private AudioClip audioClip;
    [SerializeField, Min(0f)] private float audioVolume = 1f;

    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private Vector3 vfxOffset;
    [SerializeField] private GameplayEventVfxParentMode vfxParentMode;

    [SerializeField] private CharacterState finishedState = CharacterState.Attack;

    public GameplayEventTimelineType EventType => eventType;
    public GameplayEventTargetRole TargetRole => targetRole;
    public bool FireInEditorPreview => fireInEditorPreview;
    public int DamageAmount => damageAmount;
    public bool AllowFriendlyFire => allowFriendlyFire;
    public AudioClip AudioClip => audioClip;
    public float AudioVolume => audioVolume;
    public GameObject VfxPrefab => vfxPrefab;
    public Transform VfxSpawnPoint => vfxSpawnPoint;
    public Vector3 VfxOffset => vfxOffset;
    public GameplayEventVfxParentMode VfxParentMode => vfxParentMode;
    public CharacterState FinishedState => finishedState;
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<GameplayEventTimelineBehaviour>.Create(graph);
        playable.GetBehaviour().Initialize(this);
        return playable;
    }
}
