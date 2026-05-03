using System;
using UniRx;
using UnityEngine;

public enum CharacterState
{
    Idle,
    Run,
    Attack,
    JumpStart,
    JumpFall,
    JumpLand,
    HitReact
}

public readonly struct CharacterStateChanged
{
    public CharacterStateChanged(CharacterState previousState, CharacterState nextState)
    {
        PreviousState = previousState;
        NextState = nextState;
    }

    public CharacterState PreviousState { get; }
    public CharacterState NextState { get; }
}

public class CharacterStateMachine : MonoBehaviour
{
    [SerializeField] private CharacterMovement charMovementComponent;
    [SerializeField] private CharacterTimelineAnimator charTimelineAnimator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform facingRoot;
    [SerializeField] private GameplayEventBus gameplayEventBus;
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField, Min(0f)] private float attackBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float runVelocityThreshold = 0.05f;

    private readonly Subject<CharacterStateChanged> stateChanged = new();
    private readonly CompositeDisposable subscriptions = new();
    public IObservable<CharacterStateChanged> OnStateChanged => stateChanged;

    [SerializeField] private CharacterState currentState;
    public CharacterState CurrentState => currentState;
    public bool IsAttacking => isAttacking;
    
    private bool hitReactRequested;
    private bool pendingAttackPressed;
    private bool pendingAttackReleased;
    private bool attackHeld;
    [SerializeField] private bool isAttacking;
    private bool hasCurrentState;
    private bool isObservingGameplayEvents;
    private float timeSinceAttackPressed = float.PositiveInfinity;
    private float facingDirection = 1f;

    private void Awake()
    {
        if (!charMovementComponent)
        {
            charMovementComponent = GetComponent<CharacterMovement>();
        }

        if (!charTimelineAnimator)
        {
            charTimelineAnimator = GetComponent<CharacterTimelineAnimator>();
        }

        if (!spriteRenderer)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (!healthComponent)
        {
            healthComponent = GetComponent<HealthComponent>();
        }

        BindGameplayEvents();
    }

    public void Initialize(GameplayEventBus eventBus)
    {
        gameplayEventBus = eventBus;
        BindGameplayEvents();
    }

    private void BindGameplayEvents()
    {
        if (isObservingGameplayEvents || gameplayEventBus == null)
        {
            return;
        }

        isObservingGameplayEvents = true;

        gameplayEventBus
            .Observe<AttackTimelineFinishedGameplayEvent>()
            .Where(IsOwnAttackTimelineFinishedEvent)
            .Subscribe(_ => OnAttackTimelineFinished())
            .AddTo(subscriptions);
    }

    private bool IsOwnAttackTimelineFinishedEvent(AttackTimelineFinishedGameplayEvent gameplayEvent)
    {
        return gameplayEvent.Character == gameObject
            || gameplayEvent.TimelineAnimator == charTimelineAnimator;
    }

    private void Update()
    {
        TickAttackBuffer();
        UpdateFacingDirection();
        SetState(ResolveState());
    }
    
    private void TickAttackBuffer()
    {
        if (pendingAttackPressed)
        {
            timeSinceAttackPressed = 0f;
            pendingAttackPressed = false;
            return;
        }

        timeSinceAttackPressed += Time.deltaTime;
        pendingAttackReleased = false;
    }
    
    private void SetState(CharacterState state, bool forceRestartTimeline = false)
    {
        if (hasCurrentState && currentState == state && !forceRestartTimeline)
        {
            return;
        }
        
        var previousState = currentState;
        currentState = state;
        hasCurrentState = true;
        
        charTimelineAnimator?.PlayState(state, forceRestartTimeline);
        stateChanged.OnNext(new CharacterStateChanged(previousState, currentState));
    }

    private CharacterState ResolveState()
    {
        if (hitReactRequested)
        {
            hitReactRequested = false;
            HandleAttackInterrupted();
            return CharacterState.HitReact;
        }
        
        if (isAttacking)
        {
            return CharacterState.Attack;
        }

        if (HasBufferedAttack())
        {
            BeginAttack();
            return CharacterState.Attack;
        }

        if (!charMovementComponent)
        {
            return CharacterState.Idle;
        }

        if (!charMovementComponent.IsGrounded)
        {
            return charMovementComponent.Velocity.y >= 0f
                ? CharacterState.JumpStart
                : CharacterState.JumpFall;
        }
        
        if (Mathf.Abs(charMovementComponent.Velocity.x) > runVelocityThreshold && charMovementComponent.IsGrounded)
        {
            return CharacterState.Run;
        }
        return CharacterState.Idle;
    }

    public void SetCombatInput(CombatInputFrame combatInputFrame)
    {
        pendingAttackPressed |= combatInputFrame.AttackPressed;
        pendingAttackReleased |= combatInputFrame.AttackReleased;
        attackHeld = combatInputFrame.AttackHeld;
    }

    public void RequestAttack()
    {
        pendingAttackPressed = true;
    }
    
    public void RequestHitReact(GameplayInstigator instigator)
    {
        hitReactRequested = true;
    }

    public void HandleAttackInterrupted()
    {
        isAttacking = false;
        timeSinceAttackPressed = float.PositiveInfinity;
        pendingAttackPressed = false;
        pendingAttackReleased = false;
    }

    public void HandleAttackFinished()
    {
        isAttacking = false;

        if (HasBufferedAttack())
        {
            BeginAttack();
            SetState(CharacterState.Attack, true);
            return;
        }

        SetState(ResolveState(), true);
    }

    public void OnAttackTimelineFinished()
    {
        HandleAttackFinished();
    }

    public void ResetState(CharacterState state = CharacterState.Idle)
    {
        hitReactRequested = false;
        isAttacking = false;
        timeSinceAttackPressed = float.PositiveInfinity;
        pendingAttackPressed = false;
        pendingAttackReleased = false;
        attackHeld = false;
        SetState(state, true);
    }

    private void BeginAttack()
    {
        isAttacking = true;
        timeSinceAttackPressed = float.PositiveInfinity;
        pendingAttackPressed = false;
        pendingAttackReleased = false;
    }

    private bool HasBufferedAttack()
    {
        return timeSinceAttackPressed <= attackBufferTime;
    }

    private void UpdateFacingDirection()
    {
        if (!charMovementComponent)
        {
            return;
        }

        facingDirection = charMovementComponent.FacingDirection;
        ApplyFacingDirection();
    }

    private void ApplyFacingDirection()
    {
        if (!facingRoot)
        {
            return;
        }

        var scale = facingRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingDirection < 0f ? -1f : 1f);
        facingRoot.localScale = scale;
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
        stateChanged.OnCompleted();
        stateChanged.Dispose();
    }
}
