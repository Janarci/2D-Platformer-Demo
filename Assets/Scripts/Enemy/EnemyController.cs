using UnityEngine;

public sealed class EnemyController : MonoBehaviour
{
    private enum EnemyBrainState
    {
        Idle,
        Patrol,
        Chase
    }

    [Header("References")]
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private Transform target;

    [Header("Detection")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Min(0f)] private float aggroRange = 5f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;
    [SerializeField, Min(0f)] private float verticalAttackTolerance = 0.8f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.2f;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float patrolLeashDistance = 3f;
    [SerializeField, Min(0f)] private float patrolSpeedScale = 0.45f;
    [SerializeField, Min(0f)] private float chaseSpeedScale = 1f;
    [SerializeField] private Vector2 patrolMoveDurationRange = new Vector2(1.2f, 2.8f);
    [SerializeField] private Vector2 idleDurationRange = new Vector2(0.4f, 1.4f);
    [SerializeField, Range(0f, 1f)] private float idleChanceAfterPatrol = 0.4f;

    [Header("Ground Checks")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private Transform groundAheadProbe;
    [SerializeField] private Transform wallProbe;
    [SerializeField, Min(0f)] private float groundAheadHorizontalOffset = 0.35f;
    [SerializeField, Min(0f)] private float groundAheadRayDistance = 1.2f;
    [SerializeField, Min(0f)] private float wallRayDistance = 0.25f;
    [SerializeField] private bool requireGroundAhead = true;

    private Vector2 homePosition;
    private EnemyBrainState brainState;
    private float stateTimer;
    private float attackCooldownTimer;
    private float moveDirection = 1f;

    private void Awake()
    {
        if (!enemyMovement)
        {
            enemyMovement = GetComponent<EnemyMovement>();
        }

        if (!stateMachine)
        {
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        homePosition = transform.position;
        moveDirection = Random.value < 0.5f ? -1f : 1f;
        EnterIdle();
    }

    private void Update()
    {
        if (!enemyMovement)
        {
            return;
        }

        attackCooldownTimer = Mathf.Max(0f, attackCooldownTimer - Time.deltaTime);

        if (stateMachine != null && stateMachine.IsAttacking)
        {
            enemyMovement.StopHorizontalMove();
            FaceTargetIfPossible();
            return;
        }

        var targetInAggroRange = TryResolveTarget(out var activeTarget)
            && IsTargetInsideAggroRange(activeTarget);

        if (targetInAggroRange)
        {
            UpdateAggro(activeTarget);
            return;
        }

        UpdatePatrol();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ResetBehavior()
    {
        homePosition = transform.position;
        attackCooldownTimer = 0f;
        moveDirection = Random.value < 0.5f ? -1f : 1f;
        EnterIdle();
    }

    private void UpdateAggro(Transform activeTarget)
    {
        brainState = EnemyBrainState.Chase;

        var deltaX = activeTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.01f)
        {
            enemyMovement.StopHorizontalMove();
            return;
        }

        var directionToTarget = Mathf.Sign(deltaX);
        if (IsTargetInsideAttackRange(activeTarget))
        {
            enemyMovement.StopHorizontalMove();

            if (!enemyMovement.IsFacingPosition(activeTarget.position))
            {
                enemyMovement.FaceDirection(directionToTarget);
                return;
            }

            TryAttack();
            return;
        }

        MoveIfGroundAllows(directionToTarget, chaseSpeedScale);
    }

    private void UpdatePatrol()
    {
        stateTimer -= Time.deltaTime;

        switch (brainState)
        {
            case EnemyBrainState.Idle:
                enemyMovement.StopHorizontalMove();

                if (stateTimer <= 0f)
                {
                    EnterPatrol();
                }
                break;

            case EnemyBrainState.Patrol:
                if (stateTimer <= 0f)
                {
                    if (Random.value <= idleChanceAfterPatrol)
                    {
                        EnterIdle();
                    }
                    else
                    {
                        FlipPatrolDirection();
                        EnterPatrol();
                    }

                    return;
                }

                if (!MoveIfGroundAllows(moveDirection, patrolSpeedScale))
                {
                    FlipPatrolDirection();
                    EnterIdle();
                }
                break;

            case EnemyBrainState.Chase:
                EnterIdle();
                break;
        }
    }

    private void TryAttack()
    {
        if (stateMachine == null || attackCooldownTimer > 0f)
        {
            return;
        }

        attackCooldownTimer = attackCooldown;
        stateMachine.RequestAttack();
    }

    private bool MoveIfGroundAllows(float direction, float speedScale)
    {
        if (!CanMove(direction))
        {
            enemyMovement.StopHorizontalMove();
            return false;
        }

        enemyMovement.SetHorizontalMove(Mathf.Sign(direction) * Mathf.Clamp01(speedScale));
        return true;
    }

    private bool CanMove(float direction)
    {
        if (Mathf.Abs(direction) <= 0.01f)
        {
            return false;
        }

        var signedDirection = Mathf.Sign(direction);
        if (WouldLeavePatrolGround(signedDirection))
        {
            return false;
        }

        if (requireGroundAhead && !HasGroundAhead(signedDirection))
        {
            return false;
        }

        return !HasWallAhead(signedDirection);
    }

    private bool WouldLeavePatrolGround(float direction)
    {
        if (patrolLeashDistance <= 0f)
        {
            return false;
        }

        var nextX = transform.position.x + direction * groundAheadHorizontalOffset;
        return Mathf.Abs(nextX - homePosition.x) > patrolLeashDistance;
    }

    private bool HasGroundAhead(float direction)
    {
        var probeOrigin = groundAheadProbe != null
            ? (Vector2)groundAheadProbe.position
            : (Vector2)transform.position;
        var origin = probeOrigin + Vector2.right * direction * groundAheadHorizontalOffset;

        return Physics2D.Raycast(origin, Vector2.down, groundAheadRayDistance, groundLayers);
    }

    private bool HasWallAhead(float direction)
    {
        var origin = wallProbe != null
            ? (Vector2)wallProbe.position
            : (Vector2)transform.position;

        return Physics2D.Raycast(origin, Vector2.right * direction, wallRayDistance, obstacleLayers);
    }

    private bool TryResolveTarget(out Transform activeTarget)
    {
        if (target != null && target.gameObject.activeInHierarchy && target != transform.root)
        {
            activeTarget = target;
            return true;
        }
        
        target = null;

        var hits = Physics2D.OverlapCircleAll(transform.position, aggroRange, targetLayers);
        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.root == transform.root)
            {
                continue;
            }

            activeTarget = hit.transform;
            target = activeTarget;
            return true;
        }

        activeTarget = null;
        target = null;
        return false;
    }

    private bool IsTargetInsideAggroRange(Transform activeTarget)
    {
        return Vector2.Distance(transform.position, activeTarget.position) <= aggroRange;
    }

    private bool IsTargetInsideAttackRange(Transform activeTarget)
    {
        var delta = activeTarget.position - transform.position;
        return Mathf.Abs(delta.x) <= attackRange
            && Mathf.Abs(delta.y) <= verticalAttackTolerance;
    }

    private void FaceTargetIfPossible()
    {
        if (target == null)
        {
            return;
        }

        var directionToTarget = target.position.x - transform.position.x;
        enemyMovement.FaceDirection(directionToTarget);
    }

    private void EnterIdle()
    {
        brainState = EnemyBrainState.Idle;
        stateTimer = RandomInRange(idleDurationRange);
        enemyMovement?.StopHorizontalMove();
    }

    private void EnterPatrol()
    {
        brainState = EnemyBrainState.Patrol;
        stateTimer = RandomInRange(patrolMoveDurationRange);
    }

    private void FlipPatrolDirection()
    {
        moveDirection = -moveDirection;
    }

    private static float RandomInRange(Vector2 range)
    {
        var min = Mathf.Min(range.x, range.y);
        var max = Mathf.Max(range.x, range.y);
        return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(attackRange * 2f, verticalAttackTolerance * 2f, 0f));

        var center = Application.isPlaying ? homePosition : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(center.x - patrolLeashDistance, center.y, transform.position.z),
            new Vector3(center.x + patrolLeashDistance, center.y, transform.position.z));
    }
}
