using Unity.VisualScripting;
using UnityEngine;

public class SoundCursor : MonoBehaviour
{
    // Input thresholds
    public float minLoudness = 0.2f;
    public float minFrequency = 0.2f;

    // Scaling
    [Range(0f, 1f)]
    public float yScale = 0.3f;    // fraction of viewport height
    public float xScale = 4f;


    // Options
    public bool enableFrequency = false;
    public float loudnessSmoothTime = 0.10f;

    // Internal state
    float smoothedLoudness;
    float loudnessVelocity;

    float baseY;       // world-space baseline
    float viewHeight;  // world-space height of the viewport

    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("SoundPlatform: No camera tagged MainCamera found.");
            enabled = false;
            return;
        }

        float zDist = Mathf.Abs(transform.position.z - cam.transform.position.z);

        // World-space top and bottom of the view at this depth
        Vector3 bottomWp = cam.ViewportToWorldPoint(new Vector3(0f, 0f, zDist));
        Vector3 topWp = cam.ViewportToWorldPoint(new Vector3(0f, 1f, zDist));
        viewHeight = topWp.y - bottomWp.y;

        // Baseline world-space Y at the chosen viewport height
        Vector3 baseWp = cam.ViewportToWorldPoint(new Vector3(0.5f, Globals.yOffset, zDist));
        baseY = baseWp.y;
    }

    void Update()
    {
        // Clamp raw loudness
        float rawLoud = Mathf.Max(MicCalibration.loudness, minLoudness);

        // Smooth loudness
        smoothedLoudness = Mathf.SmoothDamp(
            smoothedLoudness,
            rawLoud,
            ref loudnessVelocity,
            loudnessSmoothTime);

        // X position (frequency or fixed)
        float x = enableFrequency
            ? ((rawLoud == minLoudness ? minFrequency : MicCalibration.frequency) * xScale)
            : transform.position.x;

        // Y position: baseline plus loudness-scaled viewport fraction
        float y = baseY + smoothedLoudness * yScale * viewHeight;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
