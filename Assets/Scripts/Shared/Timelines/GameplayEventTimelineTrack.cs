using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.9f, 0.35f, 0.2f)]
[TrackClipType(typeof(GameplayEventTimelineClip))]
[TrackBindingType(typeof(GameObject))]
public sealed class GameplayEventTimelineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<GameplayEventTimelineMixer>.Create(graph, inputCount);
    }
}
