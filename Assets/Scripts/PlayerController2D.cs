// PlayerController2D.cs
// Unity 6.1 - 2D platformer controller (ASCII only)
// Requires Rigidbody2D and Collider2D on the same GameObject.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    // Input actions -------------------------------------------------------
    [Header("Input Actions")]
    public InputActionReference moveAction;  // Vector2
    public InputActionReference jumpAction;  // Button

    // Movement ------------------------------------------------------------
    [Header("Movement")]
    public float moveSpeed = 8f;      // units per second
    public float jumpImpulse = 12f;   // upward impulse

    // Ground check --------------------------------------------------------
    [Header("Ground Check")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 45f;
    public float groundProbeExtra = 0.05f;  // extra ray length below feet
    public LayerMask groundMask;

    // Private -------------------------------------------------------------
    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private Vector2 groundNormal = Vector2.up;

    // --------------------------------------------------------------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
    }

    private void Update()
    {
        // Jump (button pressed this frame and player on the ground)
        if (isGrounded &&
            jumpAction != null &&
            jumpAction.action.WasPressedThisFrame())
        {
            // Clear any downward velocity before applying the impulse
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        HandleHorizontal();
        ProbeGround();
    }

    // --------------------------------------------------------------------
    private void HandleHorizontal()
    {
        Vector2 input = moveAction != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        // Project horizontal movement along the ground tangent
        Vector2 tangent = Vector2.Perpendicular(groundNormal).normalized;
        float sign = Mathf.Sign(tangent.x == 0f ? 1f : tangent.x);
        float targetX = tangent.x * input.x * moveSpeed * sign;

        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }

    private void ProbeGround()
    {
        // Cast straight down from collider center so the ray always
        // intersects the floor even when the floor sits flush.
        Vector2 origin = col.bounds.center;
        float length = col.bounds.extents.y + groundProbeExtra; // half-height plus extra

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, length, groundMask);
#if UNITY_EDITOR
        Debug.DrawLine(origin, origin + Vector2.down * length,
            hit ? Color.green : Color.red);
#endif
        if (hit)
        {
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            isGrounded = angle <= maxSlopeAngle;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector2.up;
        }
    }
}
