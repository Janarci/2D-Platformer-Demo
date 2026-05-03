using UnityEngine;

[CreateAssetMenu(fileName = "PlatformerContext", menuName = "Platformer/Platformer Context")]
public sealed class PlatformerContext : ScriptableObject
{
    [Header("Horizontal Movement")]
    [SerializeField, Min(0f)] private float runSpeed = 8f;
    [SerializeField, Min(0f)] private float acceleration = 70f;
    [SerializeField, Min(0f)] private float deceleration = 80f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float jumpForce = 14f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

    [Header("Gravity")]
    [SerializeField, Min(0f)] private float gravityScale = 3f;
    [SerializeField, Min(0f)] private float fallGravityMultiplier = 1.8f;
    [SerializeField, Min(0f)] private float lowJumpGravityMultiplier = 2.5f;

    public float RunSpeed => runSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float JumpForce => jumpForce;
    public float CoyoteTime => coyoteTime;
    public float JumpBufferTime => jumpBufferTime;
    public float GravityScale => gravityScale;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float LowJumpGravityMultiplier => lowJumpGravityMultiplier;
}
