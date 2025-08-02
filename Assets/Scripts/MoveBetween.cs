using UnityEngine;

public class MoveBetween : MonoBehaviour
{
    public Vector2 pointA = new Vector2(-3f, 0f);
    public Vector2 pointB = new Vector2(3f, 0f);
    public float speed = 2f;
    public float endDelay = 0f;   // seconds to wait at each end
    public int loopCount = 0;    // how many times we’ve bounced

    Vector2 target;
    float delayTimer = 0f;       // counts down while waiting

    void Start()
    {
        transform.position = pointA;
        target = pointB;
    }

    void Update()
    {
        // If we’re in the waiting phase, just count down.
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;                 // skip movement this frame
        }

        // Move toward the active target.
        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime);

        // Check if we’ve arrived.
        if (Vector2.Distance(transform.position, target) < 0.01f)
        {
            // Set up the wait.
            delayTimer = endDelay;

            // Flip target for the next run.
            target = (target == pointA) ? pointB : pointA;

            loopCount++;            // completed one leg of the journey
        }
    }
}
