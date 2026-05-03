using System;
using UniRx;
using UnityEngine;

public sealed class DelayedGameplayEventScheduler : MonoBehaviour
{
    private GameplayEventBus gameplayEventBus;

    public bool IsInitialized { get; private set; }

    public IObservable<T> ScheduleAndObserve<T>(T gameplayEvent, float delaySeconds)
    where T : GameplayEvent
    {
        EnsureInitialized();

        gameplayEventBus.Register(gameplayEvent, delaySeconds);

        return gameplayEventBus
            .Observe<T>()
            .Where(firedEvent => ReferenceEquals(firedEvent, gameplayEvent))
            .Take(1);
    }
    
    
    
    public void Initialize(GameplayEventBus eventBus)
    {
        if (eventBus == null)
        {
            throw new MissingReferenceException(
                $"{nameof(DelayedGameplayEventScheduler)} requires a {nameof(GameplayEventBus)}.");
        }

        gameplayEventBus = eventBus;
        IsInitialized = true;
    }
    
    //deprecated
    public GameplayEvent Schedule(GameplayEvent gameplayEvent, float delaySeconds)
    {
        EnsureInitialized();
        gameplayEventBus.Register(gameplayEvent, delaySeconds);
        return gameplayEvent;
    }

    public GameplayEvent ScheduleImmediate(GameplayEvent gameplayEvent)
    {
        return Schedule(gameplayEvent, 0f);
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new System.InvalidOperationException(
                $"{nameof(DelayedGameplayEventScheduler)} must be initialized before scheduling events.");
        }
    }
}
