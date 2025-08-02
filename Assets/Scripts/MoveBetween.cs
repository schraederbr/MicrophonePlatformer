using UnityEngine;

public class MoveBetween : MonoBehaviour
{
    [Header("Movement settings")]
    [Tooltip("How many seconds to go from the left edge to the right edge.")]
    public float secondsPerSweep = 5f;

    [Tooltip("Pause at each end, in seconds.")]
    public float endDelay = 0f;


    // Runtime-calculated points
    public Vector2 pointA { get; private set; }
    public Vector2 pointB { get; private set; }

    Vector2 target;
    float delayTimer = 0f;
    public float legSpeed;          // units per second
    public int loopCount { get; private set; }

    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("MoveBetween: No camera tagged MainCamera found.");
            enabled = false;
            return;
        }

        // Distance from camera to this object (works for both ortho & perspective)
        float zDistance = Mathf.Abs(transform.position.z - cam.transform.position.z);

        // World-space positions of the left & right viewport edges at the chosen height
        Vector3 leftWorld = cam.ViewportToWorldPoint(new Vector3(0f, Globals.yOffset, zDistance));
        Vector3 rightWorld = cam.ViewportToWorldPoint(new Vector3(1f, Globals.yOffset, zDistance));

        pointA = new Vector2(leftWorld.x, leftWorld.y);
        pointB = new Vector2(rightWorld.x, rightWorld.y);

        // Calculate the actual movement speed (units per second)
        float legDistance = Vector2.Distance(pointA, pointB);
        if (secondsPerSweep <= 0f)
            secondsPerSweep = 0.01f;          // avoid divide-by-zero / negative
        legSpeed = legDistance / secondsPerSweep;

        // Initialise movement
        transform.position = pointA;
        target = pointB;
    }

    void Update()
    {
        // Handle end-of-leg pause
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        // Move toward the current target
        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            legSpeed * Time.deltaTime);

        // Arrived?
        if (Vector2.Distance(transform.position, target) < 0.01f)
        {
            delayTimer = endDelay;                       // start the pause
            target = (target == pointA) ? pointB : pointA;
            loopCount++;                                 // finished one leg
        }
    }
}
