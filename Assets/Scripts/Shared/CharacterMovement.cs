using UnityEngine;

[System.Flags]
public enum CharacterControlLockReason
{
    None = 0,
    Attack = 1,
    HitReact = 2
}

[RequireComponent(typeof(Rigidbody2D))]
public abstract class CharacterMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private PlatformerContext platformerContext;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayers = ~0;

    private CharacterMovementIntent currentIntent;
    private bool pendingJumpPressed;
    private bool pendingJumpReleased;
    private float timeSinceGrounded = float.PositiveInfinity;
    private float timeSinceJumpPressed = float.PositiveInfinity;
    private CharacterControlLockReason controlLocks;

    public bool IsGrounded { get; private set; }
    public bool IsControlLocked => controlLocks != CharacterControlLockReason.None;
    public Vector2 Velocity => body != null ? body.velocity : Vector2.zero;
    public float FacingDirection { get; private set; } = 1f;
    protected PlatformerContext PlatformerContext => platformerContext;
    protected Rigidbody2D Body => body;

    protected virtual void Awake()
    {
        ResolveBody();
    }

    public virtual void Initialize(PlatformerContext context)
    {
        platformerContext = context;
        ResolveBody();
    }

    public virtual void SetIntent(CharacterMovementIntent intent)
    {
        if (IsControlLocked)
        {
            ClearMovementInput();
            return;
        }
        
        pendingJumpPressed |= intent.JumpPressed;
        pendingJumpReleased |= intent.JumpReleased;
        currentIntent = new CharacterMovementIntent(
            intent.Move,
            pendingJumpPressed,
            intent.JumpHeld,
            pendingJumpReleased);

        if (Mathf.Abs(intent.Move.x) > 0.01f)
        {
            FacingDirection = Mathf.Sign(intent.Move.x);
        }
    }

    public void SetFacingDirection(float horizontalDirection)
    {
        if (IsControlLocked)
        {
            return;
        }
        
        SetFacingDirectionForced(horizontalDirection);
    }

    public void SetFacingDirectionForced(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) > 0.01f)
        {
            FacingDirection = Mathf.Sign(horizontalDirection);
        }
    }
    
    public void AddControlLock(CharacterControlLockReason reason, bool stopHorizontalVelocity = true)
    {
        if (reason == CharacterControlLockReason.None)
        {
            return;
        }

        var hadReason = (controlLocks & reason) != 0;
        controlLocks |= reason;
        ClearMovementInput();

        if (!hadReason && stopHorizontalVelocity && body != null)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
        }
    }
    
    public void RemoveControlLock(CharacterControlLockReason reason)
    {
        if (reason == CharacterControlLockReason.None)
        {
            return;
        }

        controlLocks &= ~reason;
    }

    public void ClearControlLocks()
    {
        controlLocks = CharacterControlLockReason.None;
    }
    
    protected virtual void FixedUpdate()
    {
        if (!body || !platformerContext)
        {
            return;
        }

        RefreshGrounded();
        TrackJumpWindows();
        ApplyHorizontalMovement();
        TryApplyJump();
        ApplyGravityModifiers();

        ClearJumpBuffer();
    }

    protected virtual void RefreshGrounded()
    {
        var checkPosition = groundCheck != null
            ? (Vector2)groundCheck.position
            : body.position + Vector2.down * 0.6f;

        IsGrounded = Physics2D.OverlapBox(checkPosition, groundCheckSize, 0f, groundLayers) != null;
    }
    private void OnDrawGizmosSelected()
    {
        var center = groundCheck != null
            ? (Vector2)groundCheck.position
            : body != null
                ? body.position + Vector2.down * 0.6f
                : (Vector2)transform.position + Vector2.down * 0.6f;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, groundCheckSize);
    }
    
    private void TrackJumpWindows()
    {
        timeSinceGrounded = IsGrounded ? 0f : timeSinceGrounded + Time.fixedDeltaTime;
        timeSinceJumpPressed = pendingJumpPressed ? 0f : timeSinceJumpPressed + Time.fixedDeltaTime;
    }

    private void ApplyHorizontalMovement()
    {
        if (IsControlLocked)
        {
            return;
        }
        
        var targetSpeed = currentIntent.Move.x * platformerContext.RunSpeed;
        var rate = Mathf.Abs(targetSpeed) > 0.01f
            ? platformerContext.Acceleration
            : platformerContext.Deceleration;

        var newHorizontalVelocity = Mathf.MoveTowards(
            body.velocity.x,
            targetSpeed,
            rate * Time.fixedDeltaTime);

        body.velocity = new Vector2(newHorizontalVelocity, body.velocity.y);
    }

    private void TryApplyJump()
    {
        if (IsControlLocked)
        {
            timeSinceJumpPressed = float.PositiveInfinity;
            return;
        }
        
        var canUseCoyoteTime = timeSinceGrounded <= platformerContext.CoyoteTime;
        var hasBufferedJump = timeSinceJumpPressed <= platformerContext.JumpBufferTime;

        if (!canUseCoyoteTime || !hasBufferedJump)
        {
            return;
        }

        body.velocity = new Vector2(body.velocity.x, platformerContext.JumpForce);
        timeSinceGrounded = float.PositiveInfinity;
        timeSinceJumpPressed = float.PositiveInfinity;
    }

    private void ApplyGravityModifiers()
    {
        var gravityScale = platformerContext.GravityScale;

        if (body.velocity.y < -0.01f)
        {
            gravityScale *= platformerContext.FallGravityMultiplier;
        }
        else if (body.velocity.y > 0.01f && !currentIntent.JumpHeld)
        {
            gravityScale *= platformerContext.LowJumpGravityMultiplier;
        }

        body.gravityScale = gravityScale;
    }

    private void ResolveBody()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void ClearJumpBuffer()
    {
        pendingJumpPressed = false;
        pendingJumpReleased = false;
        
        currentIntent = new CharacterMovementIntent(
            currentIntent.Move,
            false,
            currentIntent.JumpHeld,
            false);
    }
    
    private void ClearMovementInput()
    {
        pendingJumpPressed = false;
        pendingJumpReleased = false;
        currentIntent = new CharacterMovementIntent(Vector2.zero, false, false, false);
    }
}
