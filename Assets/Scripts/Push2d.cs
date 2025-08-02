using UnityEngine;
using UnityEngine.InputSystem;   // new input system

[RequireComponent(typeof(Rigidbody2D))]
public class Push2d : MonoBehaviour
{
    [SerializeField] private float pushForce = 10f;     // horizontal force
    [SerializeField] private float jumpForce = 200f;    // jump impulse

    public InputActionReference moveAction;             // Vector2  "Move"
    public InputActionReference jumpAction;             // Button   "Jump"

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Force);
        }
    }

    private void FixedUpdate()   // physics step
    {
        float h = 0f;

        if (moveAction != null)
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>(); // x in -1..1
            h = input.x;
        }

        if (Mathf.Abs(h) > 0.01f)
        {
            rb.AddForce(Vector2.right * h * pushForce, ForceMode2D.Force);
        }
    }
}
