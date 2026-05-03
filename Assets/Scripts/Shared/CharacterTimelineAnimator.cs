using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


public class CharacterTimelineAnimator : MonoBehaviour
{
    private const double TimelineEndTolerance = 0.001d;

    [Serializable]
    private struct StateTimeline
    {
        public CharacterState State;
        public TimelineAsset Timeline;
        public DirectorWrapMode WrapMode;
    }
    
    [SerializeField] private PlayableDirector directorComponent;
    [SerializeField] private StateTimeline[] stateTimelines;
    [SerializeField] private GameplayEventBus gameplayEventBus;
    
    public CharacterState currentState { get; private set; }
    private bool hasCurrentState;
    private bool timelineFinishEventSent;

    private void Awake()
    {
        if (!directorComponent)
        {
            directorComponent = GetComponent<PlayableDirector>();
        }

        if (directorComponent)
        {
            directorComponent.paused += HandleDirectorPaused;
            directorComponent.stopped += HandleDirectorStopped;
        }
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus;
    }

    public void PlayState(CharacterState state, bool forceRestart = false)
    {
        if (!directorComponent)
        {
            return;
        }

        if (forceRestart == false && hasCurrentState && currentState == state)
        {
            return;
        }

        var timeline = GetTimeline(state);
        if (!timeline)
        {
            return;
        }
        
        currentState = state;
        hasCurrentState = true;
        timelineFinishEventSent = false;
        directorComponent.playableAsset = timeline;
        directorComponent.extrapolationMode = GetWrapMode(state);
        directorComponent.time = 0;
        directorComponent.Play();
    }

    public void StopCurrentTimeline(bool suppressCompletionEvent = true)
    {
        if (!directorComponent)
        {
            return;
        }

        if (suppressCompletionEvent)
        {
            timelineFinishEventSent = true;
        }

        directorComponent.Stop();
    }

    private DirectorWrapMode GetWrapMode(CharacterState state)
    {
        foreach (var stateTimeline in stateTimelines)
        {
            if (stateTimeline.State == state)
            {
                return stateTimeline.WrapMode;
            }
        }
        return DirectorWrapMode.Loop;
    }

    private TimelineAsset GetTimeline(CharacterState state)
    {
        foreach (var stateTimeline in stateTimelines)
        {
            if (stateTimeline.State == state)
            {
                return stateTimeline.Timeline;
            }
        }
        return null;
    }

    private void HandleDirectorPaused(PlayableDirector pausedDirector)
    {
        if (pausedDirector != directorComponent || !IsAtTimelineEnd())
        {
            return;
        }

        HandleTimelineCompleted();
    }

    private void HandleDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != directorComponent)
        {
            return;
        }

        HandleTimelineCompleted();
    }

    private void HandleTimelineCompleted()
    {
        FireAttackTimelineFinishedEvent();
    }

    private void FireAttackTimelineFinishedEvent()
    {
        if (timelineFinishEventSent || currentState != CharacterState.Attack)
        {
            return;
        }

        timelineFinishEventSent = true;

        if (gameplayEventBus == null)
        {
            Debug.LogError(
                $"{nameof(CharacterTimelineAnimator)} cannot publish attack finish event because {nameof(gameplayEventBus)} is missing.",
                this);
            return;
        }

        var instigator = new GameplayInstigator(gameObject);
        var gameplayEvent = new AttackTimelineFinishedGameplayEvent(
            instigator,
            gameObject,
            this,
            currentState);

        gameplayEventBus.FireInstant(gameplayEvent);
    }

    private bool IsAtTimelineEnd()
    {
        if (!directorComponent || directorComponent.playableAsset == null)
        {
            return false;
        }

        if (directorComponent.extrapolationMode == DirectorWrapMode.Loop)
        {
            return false;
        }

        var duration = directorComponent.duration;
        return duration > 0d
            && !double.IsInfinity(duration)
            && directorComponent.time >= duration - TimelineEndTolerance;
    }

    private void OnDestroy()
    {
        if (directorComponent)
        {
            directorComponent.paused -= HandleDirectorPaused;
            directorComponent.stopped -= HandleDirectorStopped;
        }
    }
}
