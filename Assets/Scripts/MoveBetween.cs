using UnityEngine;
using UnityEngine.InputSystem;

public class MoveBetween : MonoBehaviour
{
    // ------------------------------------------------------------ //
    // Inspector settings
    // ------------------------------------------------------------ //
    [Header("Movement settings")]
    [Tooltip("Seconds to go from the left edge to the right edge.")]
    public float secondsPerSweep = 5f;

    [Tooltip("true = ping-pong A <-> B, false = A -> B then snap back to A.")]
    public bool twoDirections = true;

    [Tooltip("If true, object pauses at each end until Jump is pressed.")]
    public bool waitAtEnd = true;

    [Header("Input actions")]
    public InputActionReference recordAction; // assign Jump button (New Input System)

    // ------------------------------------------------------------ //
    // Runtime data
    // ------------------------------------------------------------ //
    public Vector2 pointA { get; private set; }
    public Vector2 pointB { get; private set; }

    private Vector2 target;
    private bool waitingForRecord = true;
    private bool pendingReset = false; // one-way mode snap flag

    public float legSpeed;  // units per second
    public int loopCount; // completed legs
    public GenerateTerrain terrainGenerator; // reference to terrain generator

    // ------------------------------------------------------------------ //
    // Instantly snaps the object to pointA and sets up for the next leg.
    // ------------------------------------------------------------------ //
    public void SnapToStart()
    {
        transform.position = pointA; // appear at the left edge
        target = pointB;             // next move will be toward B
        pendingReset = false;

        // If you normally pause at the ends, keep that behaviour
        if (waitAtEnd)
            waitingForRecord = true;
    }


    // ------------------------------------------------------------ //
    void OnEnable()
    {
        if (recordAction != null) recordAction.action.Enable();
    }

    void OnDisable()
    {
        if (recordAction != null) recordAction.action.Disable();
    }

    // ------------------------------------------------------------ //
    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("MoveBetween: No camera tagged MainCamera found.");
            enabled = false;
            return;
        }

        float zDistance = Mathf.Abs(transform.position.z - cam.transform.position.z);
        Vector3 leftWorld = cam.ViewportToWorldPoint(new Vector3(0f, Globals.yOffset, zDistance));
        Vector3 rightWorld = cam.ViewportToWorldPoint(new Vector3(1f, Globals.yOffset, zDistance));

        pointA = new Vector2(leftWorld.x, leftWorld.y);
        pointB = new Vector2(rightWorld.x, rightWorld.y);

        float legDistance = Vector2.Distance(pointA, pointB);
        secondsPerSweep = Mathf.Max(0.01f, secondsPerSweep);
        legSpeed = legDistance / secondsPerSweep;

        transform.position = pointA;
        target = pointB; // always begin moving right
    }

    // ------------------------------------------------------------ //
    void Update()
    {
        // -------------------------------------------------------- //
        // 1. Handle pause at end (wait for Jump)
        // -------------------------------------------------------- //
        if (waitingForRecord)
        {
            if (recordAction != null && recordAction.action.WasPressedThisFrame())
            {
                waitingForRecord = false;

                if (pendingReset)
                {
                    transform.position = pointA;
                    target = pointB;
                    pendingReset = false;
                }
            }
            else
            {
                return; // still waiting
            }
        }

        // -------------------------------------------------------- //
        // 2. Move toward current target
        // -------------------------------------------------------- //
        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            legSpeed * Time.deltaTime);

        // -------------------------------------------------------- //
        // 3. Arrived at target?
        // -------------------------------------------------------- //
        if (Vector2.Distance(transform.position, target) < 0.01f)
        {
            loopCount++;

            if (twoDirections)
            {
                target = (target == pointA) ? pointB : pointA;
                if (waitAtEnd) waitingForRecord = true;
            }
            else // one-way sweep (A -> B only)
            {
                if (target == pointB)
                {
                    if (waitAtEnd)
                    {
                        waitingForRecord = true;
                        pendingReset = true; // snap back after Jump
                        transform.position = pointA;
                    }
                    else
                    {
                        transform.position = pointA;
                        target = pointB;
                    }
                }
                else
                {
                    target = pointB;
                    if (waitAtEnd) waitingForRecord = true;
                }
            }
        }
    }
}
