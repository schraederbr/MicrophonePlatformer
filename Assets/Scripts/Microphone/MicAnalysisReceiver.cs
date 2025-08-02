using UnityEngine;

public class MicAnalysisReceiver : MonoBehaviour
{
    public float loudness;
    public float frequency;

#if !UNITY_WEBGL || UNITY_EDITOR
    private const int sampleSize = 2048;
    private AudioClip micClip;
    private string micDevice;
    private float[] samples = new float[sampleSize];
#endif

    void Start()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            if (Microphone.devices.Length > 0)
            {
                micDevice = Microphone.devices[0];
                micClip = Microphone.Start(micDevice, true, 1, 44100);
            }
            else
            {
                Debug.LogWarning("No microphone found");
            }
        #endif
    }

    void Update()
    {
        //Debug.Log($"Loudness: {loudness:F1} | Frequency: {frequency:F1} Hz");

#if !UNITY_WEBGL || UNITY_EDITOR
        if (micClip == null || !Microphone.IsRecording(micDevice))
                return;

            int micPosition = Microphone.GetPosition(micDevice) - sampleSize;
            if (micPosition < 0) return;

            micClip.GetData(samples, micPosition);

            // RMS for loudness
            float sum = 0f;
            for (int i = 0; i < sampleSize; i++)
            {
                sum += samples[i] * samples[i];
            }
            loudness = Mathf.Sqrt(sum / sampleSize) * 100f;  // scaled for readability

            // Dominant frequency (naive peak)
            int maxIndex = 0;
            float maxVal = 0f;
            for (int i = 0; i < sampleSize; i++)
            {
                float val = Mathf.Abs(samples[i]);
                if (val > maxVal)
                {
                    maxVal = val;
                    maxIndex = i;
                }
            }

            frequency = maxIndex * (44100f / sampleSize);
        #endif
    }

    // Called from JS in WebGL
    public void OnMicLoudness(string value)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
                if (float.TryParse(value, out float loud))
                    loudness = loud;
        #endif
    }

    public void OnMicFrequency(string value)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
                if (float.TryParse(value, out float freq))
                    frequency = freq;
        #endif
    }
}
