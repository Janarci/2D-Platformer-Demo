using UnityEngine;
using UnityEngine.Playables;

public sealed class GameplayEventTimelineBehaviour : PlayableBehaviour
{
    private GameplayEventTimelineClip clip;
    private bool hasFired;
    private bool warnedMissingGameStateManager;
    private double previousLocalTime = -1d;

    public void Initialize(GameplayEventTimelineClip timelineClip)
    {
        clip = timelineClip;
    }

    public void ProcessClip(
        Playable playable,
        FrameData info,
        GameObject actorRoot,
        float inputWeight)
    {
        if (inputWeight <= 0f)
        {
            ResetActivation();
            return;
        }

        var localTime = playable.GetTime();
        if (previousLocalTime >= 0d && localTime < previousLocalTime)
        {
            hasFired = false;
        }

        previousLocalTime = localTime;

        if (hasFired || clip == null || (!Application.isPlaying && !clip.FireInEditorPreview))
        {
            return;
        }

        hasFired = true;

        if (GameStateManager.Current == null)
        {
            WarnMissingGameStateManagerOnce();
            return;
        }

        GameStateManager.Current.TryFireTimelineClip(clip, actorRoot);
    }

    public void ResetActivation()
    {
        hasFired = false;
        warnedMissingGameStateManager = false;
        previousLocalTime = -1d;
    }

    private void WarnMissingGameStateManagerOnce()
    {
        if (warnedMissingGameStateManager)
        {
            return;
        }

        warnedMissingGameStateManager = true;
        Debug.LogWarning(
            $"{nameof(GameplayEventTimelineTrack)} requires {nameof(GameStateManager)}.{nameof(GameStateManager.Current)} to fire timeline gameplay events.");
    }

}
