using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public sealed class GameplayEventBus : MonoBehaviour
{
    private readonly List<ScheduledGameplayEvent> scheduledEvents = new();
    private readonly Subject<GameplayEvent> eventStream = new();
    private readonly Dictionary<GameObject, Subject<GameplayEvent>> targetEventStreams = new();
    
    private GameplayEventContext context;
    private int nextSequence;

    [SerializeField, Min(1)] private int maxEventsPerTick = 256;

    public bool IsInitialized { get; private set; }
    public float CurrentTime { get; private set; }
    public int PendingCount => scheduledEvents.Count;

    public IObservable<GameplayEvent> ObserveAll()
    {
        return eventStream.AsObservable();
    }

    public IObservable<T> Observe<T>() where T : GameplayEvent
    {
        return eventStream.OfType<GameplayEvent, T>();
    }

    public IObservable<T> ObserveTarget<T>(GameObject targetObject) where T : GameplayEvent
    {
        EnsureInitialized();

        if (targetObject == null)
        {
            throw new System.ArgumentNullException(nameof(targetObject));
        }

        return GetTargetStream(targetObject).OfType<GameplayEvent, T>();
    }

    public IObservable<T> ObserveTarget<T>(Component targetComponent) where T : GameplayEvent
    {
        if (targetComponent == null)
        {
            throw new System.ArgumentNullException(nameof(targetComponent));
        }

        return ObserveTarget<T>(targetComponent.gameObject);
    }
    
    public void Initialize(GameplayEventContext eventContext)
    {
        context = eventContext ?? throw new System.ArgumentNullException(nameof(eventContext));
        scheduledEvents.Clear();
        ClearTargetStreams();
        CurrentTime = 0f;
        nextSequence = 0;
        IsInitialized = true;
    }

    public void Register(GameplayEvent gameplayEvent)
    {
        EnsureInitialized();

        if (gameplayEvent == null)
        {
            throw new System.ArgumentNullException(nameof(gameplayEvent));
        }
        RegisterAt(gameplayEvent, Mathf.Max(CurrentTime, gameplayEvent.ExecuteAtTime));
    }

    public void FireInstant(GameplayEvent gameplayEvent, bool execute = false)
    {
        EnsureInitialized();

        if (gameplayEvent == null)
        {
            throw new System.ArgumentNullException(nameof(gameplayEvent));
        }

        gameplayEvent.SetExecuteAtTime(CurrentTime);
        FireOffEvent(gameplayEvent, execute);
    }

    public void Register(GameplayEvent gameplayEvent, float delaySeconds)
    {
        RegisterAt(gameplayEvent, CurrentTime + Mathf.Max(0f, delaySeconds));
    }

    public void RegisterAt(GameplayEvent gameplayEvent, float executeAtTime)
    {
        EnsureInitialized();

        if (gameplayEvent == null)
        {
            throw new System.ArgumentNullException(nameof(gameplayEvent));
        }

        gameplayEvent.SetExecuteAtTime(Mathf.Max(0f, executeAtTime));
        InsertSorted(new ScheduledGameplayEvent(gameplayEvent, nextSequence++));
    }

    public void Tick(float deltaTime)
    {
        EnsureInitialized();

        CurrentTime += Mathf.Max(0f, deltaTime);
        var executedThisTick = 0;

        while (scheduledEvents.Count > 0 && scheduledEvents[0].Event.ExecuteAtTime <= CurrentTime)
        {
            if (executedThisTick >= maxEventsPerTick)
            {
                Debug.LogError(
                    $"{nameof(GameplayEventBus)} Max # events already fired per tick(256)");
                return;
            }

            var scheduledEvent = scheduledEvents[0];
            scheduledEvents.RemoveAt(0);
            executedThisTick++;

            FireOffEvent(scheduledEvent.Event, false);
        }
    }

    public void Clear()
    {
        scheduledEvents.Clear();
    }

    public void Dispose()
    {
        eventStream.OnCompleted();
        eventStream.Dispose();
        ClearTargetStreams();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void InsertSorted(ScheduledGameplayEvent scheduledEvent)
    {
        var insertIndex = scheduledEvents.Count;

        for (var i = 0; i < scheduledEvents.Count; i++)
        {
            var existing = scheduledEvents[i];
            
            var earlierTime = scheduledEvent.Event.ExecuteAtTime < existing.Event.ExecuteAtTime;
            var sameTimeEarlierSequence =
                Mathf.Approximately(scheduledEvent.Event.ExecuteAtTime, existing.Event.ExecuteAtTime)
                && scheduledEvent.Sequence < existing.Sequence;

            if (earlierTime || sameTimeEarlierSequence)
            {
                insertIndex = i;
                break;
            }
        }

        scheduledEvents.Insert(insertIndex, scheduledEvent);
    }

    private void FireOffEvent(GameplayEvent gameplayEvent, bool execute)
    {
        if (!gameplayEvent.CanExecute(context))
        {
            return;
        }

        if (execute)
        {
            gameplayEvent.Execute(context);
        }
        
        PublishToTargetStream(gameplayEvent);
        eventStream.OnNext(gameplayEvent);
    }

    private Subject<GameplayEvent> GetTargetStream(GameObject targetObject)
    {
        if (!targetEventStreams.TryGetValue(targetObject, out var targetStream))
        {
            targetStream = new Subject<GameplayEvent>();
            targetEventStreams.Add(targetObject, targetStream);
        }

        return targetStream;
    }

    private void PublishToTargetStream(GameplayEvent gameplayEvent)
    {
        if (!gameplayEvent.HasTarget
            || gameplayEvent.TargetObject == null
            || !targetEventStreams.TryGetValue(gameplayEvent.TargetObject, out var targetStream))
        {
            return;
        }

        targetStream.OnNext(gameplayEvent);
    }

    private void ClearTargetStreams()
    {
        foreach (var targetStream in targetEventStreams.Values)
        {
            targetStream.OnCompleted();
            targetStream.Dispose();
        }

        targetEventStreams.Clear();
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new System.InvalidOperationException(
                $"{nameof(GameplayEventBus)} must be initialized before use.");
        }
    }

    private readonly struct ScheduledGameplayEvent
    {
        public ScheduledGameplayEvent(GameplayEvent gameplayEvent, int sequence)
        {
            Event = gameplayEvent;
            Sequence = sequence;
        }

        public GameplayEvent Event { get; }
        public int Sequence { get; }
    }
}
