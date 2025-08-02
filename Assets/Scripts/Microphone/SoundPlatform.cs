using UnityEngine;

public class SoundPlatform : MonoBehaviour
{
    [Header("Input thresholds")]
    public float minLoudness = 0.2f;
    public float minFrequency = 0.2f;

    [Header("Scaling")]
    public float yScale = 4f;
    public float xScale = 4f;

    [Header("Options")]
    public bool enableFrequency = false;
    public float loudnessSmoothTime = 0.10f;   // seconds to settle (tweak in Inspector)

    // --------------------------------------------------------------------
    private float smoothedLoudness;    // filtered value
    private float loudnessVelocity;    // internal to SmoothDamp

    void Update()
    {
        // 1. Clamp raw loudness
        float rawLoud = Mathf.Max(MicCalibration.loudness, minLoudness);

        // 2. Smooth it: after ~loudnessSmoothTime seconds it’s ~63 % closer to raw value
        smoothedLoudness = Mathf.SmoothDamp(
            smoothedLoudness,          // current value
            rawLoud,                  // target
            ref loudnessVelocity,     // ref velocity storage
            loudnessSmoothTime        // time to reach target
        );

        // 3. X position (frequency) – unchanged
        float x = enableFrequency
            ? ((rawLoud == minLoudness ? minFrequency : MicCalibration.frequency) * xScale)
            : transform.position.x;

        // 4. Y position uses the smoothed loudness
        float y = smoothedLoudness * yScale;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
