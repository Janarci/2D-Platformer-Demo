using System;
using UniRx;
using UnityEngine;

public enum CharacterState
{
    Idle,
    Run,
    Attack,
    AttackUp,
    JumpAttack,
    JumpAttackUp,
    JumpAttackDown,
    JumpStart,
    JumpFall,
    JumpLand,
    HitReact
}

public enum CharacterAttackDirection
{
    Forward,
    Up,
    Down
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
    [SerializeField] private CombatComponent combatComponent;
    [SerializeField, Min(0f)] private float attackBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float runVelocityThreshold = 0.05f;

    private readonly Subject<CharacterStateChanged> stateChanged = new();
    private readonly CompositeDisposable subscriptions = new();
    public IObservable<CharacterStateChanged> OnStateChanged => stateChanged;

    
    [SerializeField] private CharacterState currentState;
    public CharacterState CurrentState => currentState;
    private CharacterState currentAttackState = CharacterState.Attack;
    public bool IsAttacking => isAttacking;
    
    private CharacterAttackDirection currentAttackDirection = CharacterAttackDirection.Forward;
    private Vector2 currentAttackAimVector = Vector2.right;
    public CharacterAttackDirection CurrentAttackDirection => currentAttackDirection;
    public Vector2 CurrentAttackAimVector => currentAttackAimVector;
    
    private bool hitReactRequested;
    private bool pendingAttackPressed;
    private bool pendingAttackReleased;
    private bool attackHeld;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isHitReacting;
    private bool hasCurrentState;
    private bool isObservingGameplayEvents;
    private float timeSinceAttackPressed = float.PositiveInfinity;
    private float facingDirection = 1f;
    private Vector2 latestMovementAimInput;
    
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
        
        if (!combatComponent)
        {
            combatComponent = GetComponent<CombatComponent>();
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
            .Observe<CharacterStateTimelineFinishedGameplayEvent>()
            .Where(IsOwnStateTimelineFinishedEvent)
            .Subscribe(gameplayEvent => HandleStateTimelineFinished(gameplayEvent.FinishedState))
            .AddTo(subscriptions);

        gameplayEventBus
            .Observe<AttackTimelineFinishedGameplayEvent>()
            .Where(IsOwnAttackTimelineFinishedEvent)
            .Subscribe(gameplayEvent => HandleStateTimelineFinished(gameplayEvent.FinishedState))
            .AddTo(subscriptions);
    }

    private bool IsOwnStateTimelineFinishedEvent(CharacterStateTimelineFinishedGameplayEvent gameplayEvent)
    {
        return gameplayEvent.Character == gameObject
            || gameplayEvent.TimelineAnimator == charTimelineAnimator;
    }
    private bool IsOwnAttackTimelineFinishedEvent(AttackTimelineFinishedGameplayEvent gameplayEvent)
    {
        return gameplayEvent.Character == gameObject
            || gameplayEvent.TimelineAnimator == charTimelineAnimator;
    }

    private void Update()
    {
        TickAttackBuffer();
        SetState(ResolveState());
        UpdateFacingDirection();
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
        
        var timelineStarted = charTimelineAnimator != null
            && charTimelineAnimator.TryPlayState(state, forceRestartTimeline);
        
        stateChanged.OnNext(new CharacterStateChanged(previousState, currentState));
        
        if (RequiresTimelineCompletion(state) && !timelineStarted)
        {
            Debug.LogWarning(
                $"{nameof(CharacterStateMachine)} entered {state} without a playable timeline. Finishing the state immediately.",
                this);
            HandleStateTimelineFinished(state);
        }
        
    }

    private CharacterState ResolveState()
    {
        if (hitReactRequested)
        {
            BeginHitReact();
            return CharacterState.HitReact;
        }
        
        if (isHitReacting)
        {
            return CharacterState.HitReact;
        }
        
        if (isAttacking)
        {
            return currentAttackState;
        }

        if (HasBufferedAttack())
        {
            BeginAttack();
            return currentAttackState;
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

    public void SetMovementAimInput(Vector2 moveInput)
    {
        latestMovementAimInput = Vector2.ClampMagnitude(moveInput, 1f);
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
        combatComponent?.SetAttackHitboxActive(false);
        charMovementComponent?.RemoveControlLock(CharacterControlLockReason.Attack);
        charTimelineAnimator?.StopCurrentTimeline();
        timeSinceAttackPressed = float.PositiveInfinity;
        pendingAttackPressed = false;
        pendingAttackReleased = false;
        
    }

    public void HandleAttackFinished()
    {
        isAttacking = false;
        //todo: change to array of hitbox
        combatComponent?.SetAttackHitboxActive(false);
        charMovementComponent?.RemoveControlLock(CharacterControlLockReason.Attack);
        
        if (isHitReacting)
        {
            return;
        }
        
        if (HasBufferedAttack())
        {
            BeginAttack();
            SetState(CharacterState.Attack, true);
            return;
        }

        SetState(ResolveState(), true);
    }

    public void HandleStateTimelineFinished(CharacterState finishedState)
    {
        switch (finishedState)
        {
        case CharacterState.Attack:
        case CharacterState.AttackUp:
        case CharacterState.JumpAttack:
        case CharacterState.JumpAttackUp:
        case CharacterState.JumpAttackDown:
            HandleAttackFinished();
        break;

        case CharacterState.HitReact:
            HandleHitReactFinished();
        break;
        }
    }
    
    public void OnAttackTimelineFinished()
    {
        HandleAttackFinished();
    }

    public void ResetState(CharacterState state = CharacterState.Idle)
    {
        hitReactRequested = false;
        isAttacking = false;
        isHitReacting = false;
        timeSinceAttackPressed = float.PositiveInfinity;
        pendingAttackPressed = false;
        pendingAttackReleased = false;
        attackHeld = false;
        combatComponent?.SetAttackHitboxActive(false);
        charMovementComponent?.ClearControlLocks();
        SetState(state, true);
    }

    private void BeginAttack()
    {
        SnapshotAttackDirection();
        currentAttackState = GetAttackState();
        isAttacking = true;
        isHitReacting = false;
        charMovementComponent?.AddControlLock(CharacterControlLockReason.Attack, false);
        timeSinceAttackPressed = float.PositiveInfinity;
        pendingAttackPressed = false;
        pendingAttackReleased = false;
    }

    private bool HasBufferedAttack()
    {
        return timeSinceAttackPressed <= attackBufferTime;
    }
    
    private void BeginHitReact()
    {
        hitReactRequested = false;
        if (isHitReacting)
        {
            return;
        }

        HandleAttackInterrupted();
        isHitReacting = true;
        charMovementComponent?.AddControlLock(CharacterControlLockReason.HitReact, false);
    }
    
    private void HandleHitReactFinished()
    {
        if (!isHitReacting && currentState != CharacterState.HitReact)
        {
            return;
        }

        isHitReacting = false;
        hitReactRequested = false;
        charMovementComponent?.RemoveControlLock(CharacterControlLockReason.HitReact);
        SetState(ResolveState(), true);
    }
    
    private void SnapshotAttackDirection()
    {
        currentAttackDirection = ResolveAttackDirection(latestMovementAimInput);
        facingDirection = GetFacingRootDirection();
        charMovementComponent?.SetFacingDirectionForced(facingDirection);

        currentAttackAimVector = ResolveAttackAimVector(currentAttackDirection);
        ApplyFacingDirection();
    }
    
    private CharacterAttackDirection ResolveAttackDirection(Vector2 aimInput)
    {
        if (aimInput.y > 0.5f)
        {
            return CharacterAttackDirection.Up;
        }

        if (charMovementComponent != null && !charMovementComponent.IsGrounded && aimInput.y < -0.5f)
        {
            return CharacterAttackDirection.Down;
        }

        return CharacterAttackDirection.Forward;
    }
    
    private Vector2 ResolveAttackAimVector(CharacterAttackDirection attackDirection)
    {
        switch (attackDirection)
        {
        case CharacterAttackDirection.Up:
            return Vector2.up;

        case CharacterAttackDirection.Down:
            return Vector2.down;

        case CharacterAttackDirection.Forward:
        default:
            var horizontal = GetFacingRootDirection();
            return new Vector2(horizontal < 0f ? -1f : 1f, 0f);
        }
    }
    
    private float GetFacingRootDirection()
    {
        if (facingRoot)
        {
            return facingRoot.localScale.x < 0f ? -1f : 1f;
        }

        if (Mathf.Abs(facingDirection) > 0.01f)
        {
            return facingDirection < 0f ? -1f : 1f;
        }

        return charMovementComponent != null && charMovementComponent.FacingDirection < 0f ? -1f : 1f;
    }
    
    private void UpdateFacingDirection()
    {
        if (!charMovementComponent || isAttacking || isHitReacting)
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
    
    private CharacterState GetAttackState()
    {
        var isGrounded = charMovementComponent == null || charMovementComponent.IsGrounded;
        switch (currentAttackDirection)
        {
        case CharacterAttackDirection.Up:
            return isGrounded ? CharacterState.AttackUp : CharacterState.JumpAttackUp;

        case CharacterAttackDirection.Down:
            return isGrounded ? CharacterState.Attack : CharacterState.JumpAttackDown;

        case CharacterAttackDirection.Forward:
        default:
            return isGrounded ? CharacterState.Attack : CharacterState.JumpAttack;
        }
    }

    private static bool RequiresTimelineCompletion(CharacterState state)
    {
        return IsAttackState(state) || state == CharacterState.HitReact;
    }
    
    private static bool IsAttackState(CharacterState state)
    {
        return state == CharacterState.Attack
            || state == CharacterState.AttackUp
            || state == CharacterState.JumpAttack
            || state == CharacterState.JumpAttackUp
            || state == CharacterState.JumpAttackDown;
    }
    private void OnDestroy()
    {
        subscriptions.Dispose();
        stateChanged.OnCompleted();
        stateChanged.Dispose();
    }
}
